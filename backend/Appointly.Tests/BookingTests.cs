using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Appointly.Api.Data;
using Appointly.Api.DTOs;
using Appointly.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Appointly.Tests;

public sealed class BookingTests
{
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.Parse("2030-01-07T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static async Task<HttpClient> Patient(AuthFactory factory)
    {
        var client = factory.Browser();
        var email = $"patient-{Guid.NewGuid():N}@example.test";
        var password = $"Aa1!{Guid.NewGuid():N}";
        var registered = await Send(client, HttpMethod.Post, "/api/auth/register",
            new { fullName = "Booking Patient", email, password, confirmPassword = password });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var login = await Send(client, HttpMethod.Post, "/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private static async Task<HttpRequestMessage> Request(HttpClient client, HttpMethod method, string path, object? body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return request;
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        using var request = await Request(client, method, path, body);
        return await client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> Book(HttpClient client, string timestamp = "2030-01-07T01:00:00Z") =>
        Send(client, HttpMethod.Post, "/api/appointments", new { appointmentDateTimeUtc = timestamp, reasonForVisit = "Routine visit" });

    private static async Task<int> BookId(HttpClient client)
    {
        var response = await Book(client);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Booking_persists_utc_pending_and_enforces_patient_ownership()
    {
        using var factory = new AuthFactory(timeProvider: new TestClock());
        using var patient = await Patient(factory);
        using var other = await Patient(factory);
        var response = await Book(patient, "2030-01-07T09:00:00+08:00");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var booking = await patient.GetFromJsonAsync<JsonElement>(response.Headers.Location);
        var id = booking.GetProperty("id").GetInt32();
        Assert.Equal("Pending", booking.GetProperty("status").GetString());
        Assert.Equal("2030-01-07T01:00:00Z", booking.GetProperty("appointmentDateTimeUtc").GetString());
        Assert.Equal(30, booking.GetProperty("durationMinutes").GetInt32());
        Assert.True(booking.GetProperty("canCancel").GetBoolean());
        Assert.Single((await patient.GetFromJsonAsync<JsonElement>("/api/appointments/my")).EnumerateArray());
        Assert.Empty((await other.GetFromJsonAsync<JsonElement>("/api/appointments/my")).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/appointments/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(other, HttpMethod.Patch, $"/api/appointments/{id}/cancel")).StatusCode);
    }

    [Theory]
    [InlineData("2030-01-06T01:00:00Z")] // Past.
    [InlineData("2030-01-07T00:00:00Z")] // Exactly now / before opening.
    [InlineData("2030-01-07T00:30:00Z")] // Before opening.
    [InlineData("2030-01-07T09:00:00Z")] // Closing time.
    [InlineData("2030-01-07T01:15:00Z")] // Misaligned slot.
    [InlineData("2030-01-07T01:00:00.001Z")] // Sub-second misalignment.
    [InlineData("2030-01-12T01:00:00Z")] // Saturday.
    [InlineData("2030-01-07T09:00:00")] // No timezone.
    [InlineData("not-a-date")]
    public async Task Invalid_booking_times_are_rejected(string timestamp)
    {
        using var factory = new AuthFactory(timeProvider: new TestClock());
        using var patient = await Patient(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await Book(patient, timestamp)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
        Assert.Empty(await database.Appointments.ToListAsync());
        Assert.Empty(await database.SlotReservations.ToListAsync());
    }

    [Fact]
    public async Task Availability_uses_manila_day_boundaries_hours_and_reservations()
    {
        var clock = new TestClock();
        using var factory = new AuthFactory(timeProvider: clock);
        using var patient = await Patient(factory);
        var available = await patient.GetFromJsonAsync<JsonElement>("/api/availability?date=2030-01-07");
        Assert.Equal("Asia/Manila", available.GetProperty("timezone").GetString());
        var slots = available.GetProperty("slots");
        Assert.Equal(16, slots.GetArrayLength());
        Assert.Equal("09:00", slots[0].GetProperty("localTime").GetString());
        Assert.Equal("2030-01-07T01:00:00Z", slots[0].GetProperty("startUtc").GetString());
        Assert.Equal("16:30", slots[15].GetProperty("localTime").GetString());
        await BookId(patient);
        var reserved = await patient.GetFromJsonAsync<JsonElement>("/api/availability?date=2030-01-07");
        Assert.Equal(15, reserved.GetProperty("slots").GetArrayLength());
        Assert.Equal("09:30", reserved.GetProperty("slots")[0].GetProperty("localTime").GetString());
        clock.Now = DateTimeOffset.Parse("2030-01-07T01:30:00Z");
        var later = await patient.GetFromJsonAsync<JsonElement>("/api/availability?date=2030-01-07");
        Assert.Equal("10:00", later.GetProperty("slots")[0].GetProperty("localTime").GetString());
        clock.Now = DateTimeOffset.Parse("2030-01-07T16:00:00Z"); // Midnight Jan 8 in Manila.
        var past = await patient.GetFromJsonAsync<JsonElement>("/api/availability?date=2030-01-07");
        Assert.Empty(past.GetProperty("slots").EnumerateArray());
        var next = await patient.GetFromJsonAsync<JsonElement>("/api/availability?date=2030-01-08");
        Assert.Equal(16, next.GetProperty("slots").GetArrayLength());
    }

    [Fact]
    public async Task Blocked_dates_and_closed_schedules_prevent_booking()
    {
        using var factory = new AuthFactory(timeProvider: new TestClock());
        using var patient = await Patient(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
            database.BlockedDates.Add(new BlockedDate { Date = new DateOnly(2030, 1, 7) });
            (await database.ClinicSchedules.SingleAsync(item => item.DayOfWeek == DayOfWeek.Tuesday)).IsOpen = false;
            await database.SaveChangesAsync();
        }
        foreach (var date in new[] { "2030-01-07", "2030-01-08" })
        {
            var available = await patient.GetFromJsonAsync<JsonElement>($"/api/availability?date={date}");
            Assert.Empty(available.GetProperty("slots").EnumerateArray());
            Assert.Equal(HttpStatusCode.BadRequest, (await Book(patient, $"{date}T01:00:00Z")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await patient.GetAsync("/api/availability?date=invalid")).StatusCode);
    }

    [Fact]
    public async Task Duplicate_booking_conflicts_and_cancellation_releases_slot_atomically()
    {
        using var factory = new AuthFactory(timeProvider: new TestClock());
        using var patient = await Patient(factory);
        using var other = await Patient(factory);
        var id = await BookId(patient);
        Assert.Equal(HttpStatusCode.Conflict, (await Book(other)).StatusCode);
        var cancellation = await Send(patient, HttpMethod.Patch, $"/api/appointments/{id}/cancel");
        Assert.Equal(HttpStatusCode.OK, cancellation.StatusCode);
        var cancelled = await cancellation.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());
        Assert.False(cancelled.GetProperty("canCancel").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, cancelled.GetProperty("cancelledAtUtc").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(patient, HttpMethod.Patch, $"/api/appointments/{id}/cancel")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Book(other)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
        Assert.Equal(2, await database.Appointments.CountAsync());
        Assert.Single(await database.SlotReservations.ToListAsync());
    }

    [Fact]
    public async Task Concurrent_requests_have_one_winner_and_one_conflict_without_orphans()
    {
        using var factory = new AuthFactory(timeProvider: new TestClock());
        using var first = await Patient(factory);
        using var second = await Patient(factory);
        var body = new { appointmentDateTimeUtc = "2030-01-07T01:00:00Z" };
        using var firstRequest = await Request(first, HttpMethod.Post, "/api/appointments", body);
        using var secondRequest = await Request(second, HttpMethod.Post, "/api/appointments", body);
        using var barrier = new Barrier(2);
        async Task<HttpResponseMessage> Compete(HttpClient client, HttpRequestMessage request)
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(15))) throw new TimeoutException("Concurrent test did not start.");
            return await client.SendAsync(request);
        }
        var results = await Task.WhenAll(
            Task.Run(() => Compete(first, firstRequest)),
            Task.Run(() => Compete(second, secondRequest)));
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.Conflict);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
        Assert.Single(await database.Appointments.ToListAsync());
        Assert.Single(await database.SlotReservations.ToListAsync());
    }

    [Fact]
    public async Task SQLite_constraint_rejects_duplicate_reservations_even_without_service_checks()
    {
        using var factory = new AuthFactory(timeProvider: new TestClock());
        using var patient = await Patient(factory);
        await BookId(patient);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
        var original = await database.Appointments.AsNoTracking().SingleAsync();
        database.Appointments.Add(new Appointment
        {
            PatientId = original.PatientId, AppointmentDateTimeUtc = original.AppointmentDateTimeUtc,
            CreatedAtUtc = original.CreatedAtUtc, UpdatedAtUtc = original.UpdatedAtUtc,
            Reservation = new SlotReservation { SlotStartUtc = original.AppointmentDateTimeUtc }
        });
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
        Assert.Equal(19, Assert.IsType<SqliteException>(exception.InnerException).SqliteErrorCode);
        database.ChangeTracker.Clear();
        Assert.Single(await database.Appointments.ToListAsync());
        Assert.Single(await database.SlotReservations.ToListAsync());
    }

    [Theory]
    [InlineData(AppointmentStatus.Completed, false)]
    [InlineData(AppointmentStatus.Pending, true)]
    [InlineData(AppointmentStatus.Confirmed, true)]
    public async Task Historical_or_started_appointments_cannot_be_cancelled(AppointmentStatus status, bool advanceTime)
    {
        var clock = new TestClock();
        using var factory = new AuthFactory(timeProvider: clock);
        using var patient = await Patient(factory);
        var id = await BookId(patient);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
            var item = await database.Appointments.SingleAsync();
            item.Status = status;
            await database.SaveChangesAsync();
        }
        if (advanceTime) clock.Now = DateTimeOffset.Parse("2030-01-07T01:00:00Z");
        Assert.Equal(HttpStatusCode.Conflict, (await Send(patient, HttpMethod.Patch, $"/api/appointments/{id}/cancel")).StatusCode);
    }

    [Fact]
    public async Task Future_confirmed_appointment_can_be_cancelled()
    {
        using var factory = new AuthFactory(timeProvider: new TestClock());
        using var patient = await Patient(factory);
        var id = await BookId(patient);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
            (await database.Appointments.SingleAsync()).Status = AppointmentStatus.Confirmed;
            await database.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await Send(patient, HttpMethod.Patch, $"/api/appointments/{id}/cancel")).StatusCode);
    }

    [Theory]
    [InlineData(AppointmentStatus.Pending, -1, true, true)]
    [InlineData(AppointmentStatus.Pending, 0, true, false)]
    [InlineData(AppointmentStatus.Pending, 15, true, false)]
    [InlineData(AppointmentStatus.Pending, 30, false, false)]
    [InlineData(AppointmentStatus.Pending, 60, false, false)]
    [InlineData(AppointmentStatus.Confirmed, -1, true, true)]
    [InlineData(AppointmentStatus.Completed, -1, false, false)]
    [InlineData(AppointmentStatus.Cancelled, -1, false, false)]
    public async Task Patient_overview_partitions_at_visit_end_without_changing_status(
        AppointmentStatus status, int minutesAfterStart, bool isUpcoming, bool canCancel)
    {
        var clock = new TestClock();
        using var factory = new AuthFactory(timeProvider: clock);
        using var patient = await Patient(factory);
        var id = await BookId(patient);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
            (await database.Appointments.SingleAsync()).Status = status;
            await database.SaveChangesAsync();
        }
        clock.Now = DateTimeOffset.Parse("2030-01-07T01:00:00Z").AddMinutes(minutesAfterStart);
        var overview = (await patient.GetFromJsonAsync<PatientOverviewResponse>("/api/appointments/my/overview"))!;
        var item = Assert.Single(isUpcoming ? overview.Upcoming : overview.History);
        Assert.Empty(isUpcoming ? overview.History : overview.Upcoming);
        Assert.Equal(id, item.Id);
        Assert.Equal(status.ToString(), item.Status);
        Assert.Equal(canCancel, item.CanCancel);
        using var check = factory.Services.CreateScope();
        Assert.Equal(status, (await check.ServiceProvider.GetRequiredService<AppointlyDbContext>()
            .Appointments.SingleAsync()).Status);
    }

    [Fact]
    public async Task Patient_overview_orders_lists_and_never_exposes_another_patient()
    {
        using var factory = new AuthFactory(timeProvider: new TestClock());
        using var patient = await Patient(factory);
        using var other = await Patient(factory);
        var empty = (await patient.GetFromJsonAsync<PatientOverviewResponse>("/api/appointments/my/overview"))!;
        Assert.Empty(empty.Upcoming);
        Assert.Empty(empty.History);
        var profile = await patient.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var otherProfile = await other.GetFromJsonAsync<JsonElement>("/api/auth/me");
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
            var start = DateTime.Parse("2030-01-07T00:00:00Z").ToUniversalTime();
            foreach (var offset in new[] { 180, -60, 60, -180 })
                database.Appointments.Add(new Appointment {
                    PatientId = profile.GetProperty("id").GetString()!,
                    AppointmentDateTimeUtc = start.AddMinutes(offset),
                    Status = AppointmentStatus.Confirmed
                });
            database.Appointments.Add(new Appointment {
                PatientId = otherProfile.GetProperty("id").GetString()!,
                AppointmentDateTimeUtc = start.AddMinutes(120),
                ReasonForVisit = "Other patient private reason"
            });
            await database.SaveChangesAsync();
        }
        var overview = (await patient.GetFromJsonAsync<PatientOverviewResponse>("/api/appointments/my/overview"))!;
        Assert.Equal(2, overview.Upcoming.Count);
        Assert.Equal(2, overview.History.Count);
        Assert.True(overview.Upcoming[0].AppointmentDateTimeUtc < overview.Upcoming[1].AppointmentDateTimeUtc);
        Assert.True(overview.History[0].AppointmentDateTimeUtc > overview.History[1].AppointmentDateTimeUtc);
        Assert.All(overview.Upcoming.Concat(overview.History), item => Assert.Null(item.ReasonForVisit));
        var otherView = (await other.GetFromJsonAsync<PatientOverviewResponse>("/api/appointments/my/overview"))!;
        Assert.Single(otherView.Upcoming);
        Assert.Empty(otherView.History);
        Assert.DoesNotContain(otherView.Upcoming[0].Id, overview.Upcoming.Select(item => item.Id));
    }

    [Fact]
    public async Task Patient_cancellation_moves_visit_to_history_and_preserves_details()
    {
        using var factory = new AuthFactory(timeProvider: new TestClock());
        using var patient = await Patient(factory);
        var id = await BookId(patient);
        Assert.Single((await patient.GetFromJsonAsync<PatientOverviewResponse>("/api/appointments/my/overview"))!.Upcoming);
        Assert.Equal(HttpStatusCode.OK, (await Send(patient, HttpMethod.Patch, $"/api/appointments/{id}/cancel")).StatusCode);
        var overview = (await patient.GetFromJsonAsync<PatientOverviewResponse>("/api/appointments/my/overview"))!;
        Assert.Empty(overview.Upcoming);
        var history = Assert.Single(overview.History);
        Assert.Equal(id, history.Id);
        Assert.Equal("Cancelled", history.Status);
        Assert.Equal("Routine visit", history.ReasonForVisit);
        Assert.NotNull(history.CancelledAtUtc);
        Assert.False(history.CanCancel);
    }

    [Fact]
    public async Task Booking_requires_patient_role_csrf_and_server_owned_patient_identity()
    {
        using var factory = new AuthFactory("Development", seedAdmin: true, timeProvider: new TestClock());
        using var anonymous = factory.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/appointments/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/appointments/my/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/availability?date=2030-01-07")).StatusCode);
        using var patient = await Patient(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await patient.PostAsJsonAsync("/api/appointments",
            new { appointmentDateTimeUtc = "2030-01-07T01:00:00Z" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(patient, HttpMethod.Post, "/api/appointments",
            new { appointmentDateTimeUtc = "2030-01-07T01:00:00Z", patientId = "someone-else" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(patient, HttpMethod.Post, "/api/appointments",
            new { appointmentDateTimeUtc = "2030-01-07T01:00:00Z", reasonForVisit = new string('x', 201) })).StatusCode);
        var id = await BookId(patient);
        Assert.Equal(HttpStatusCode.BadRequest, (await patient.PatchAsync($"/api/appointments/{id}/cancel", null)).StatusCode);
        using var admin = factory.Browser();
        await Send(admin, HttpMethod.Post, "/api/auth/login", new { email = factory.AdminEmail, password = factory.AdminPassword });
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/appointments/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/appointments/my/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Book(admin)).StatusCode);
    }
}
