using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Appointly.Api.Data;
using Appointly.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Appointly.Tests;

public sealed class AdminTests
{
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.Parse("2030-01-07T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static async Task<HttpRequestMessage> Request(HttpClient client, HttpMethod method, string path, object? body = null)
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

    private static async Task<HttpClient> Admin(AuthFactory factory)
    {
        var client = factory.Browser();
        Assert.Equal(HttpStatusCode.OK, (await Send(client, HttpMethod.Post, "/api/auth/login",
            new { email = factory.AdminEmail, password = factory.AdminPassword })).StatusCode);
        return client;
    }

    private static async Task<HttpClient> Patient(AuthFactory factory)
    {
        var client = factory.Browser();
        var email = $"patient-{Guid.NewGuid():N}@example.test";
        var password = $"Aa1!{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created, (await Send(client, HttpMethod.Post, "/api/auth/register",
            new { fullName = "Demo Patient", email, password, confirmPassword = password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(client, HttpMethod.Post, "/api/auth/login", new { email, password })).StatusCode);
        return client;
    }

    private static AuthFactory Factory(TestClock? clock = null) => new("Development", true, clock ?? new TestClock());

    private static async Task<int> Seed(AuthFactory factory, HttpClient patient,
        AppointmentStatus status = AppointmentStatus.Pending, string start = "2030-01-07T01:00:00Z")
    {
        var profile = await patient.GetFromJsonAsync<JsonElement>("/api/auth/me");
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
        var utc = DateTime.Parse(start, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        var item = new Appointment
        {
            PatientId = profile.GetProperty("id").GetString()!, AppointmentDateTimeUtc = utc,
            CreatedAtUtc = utc.AddDays(-1), UpdatedAtUtc = utc.AddDays(-1), Status = status,
            Reservation = status is AppointmentStatus.Pending or AppointmentStatus.Confirmed
                ? new SlotReservation { SlotStartUtc = utc } : null
        };
        database.Appointments.Add(item);
        await database.SaveChangesAsync();
        return item.Id;
    }

    [Fact]
    public async Task Dashboard_and_filters_use_real_records_and_Manila_date_boundaries()
    {
        using var factory = Factory();
        using var admin = await Admin(factory);
        using var patient = await Patient(factory);
        await Seed(factory, patient, AppointmentStatus.Pending, "2030-01-06T16:00:00Z"); // Jan 7 Manila.
        await Seed(factory, patient, AppointmentStatus.Confirmed, "2030-01-07T01:00:00Z");
        await Seed(factory, patient, AppointmentStatus.Completed, "2030-01-07T15:30:00Z");
        await Seed(factory, patient, AppointmentStatus.Cancelled, "2030-01-07T16:00:00Z"); // Jan 8 Manila.
        var dashboard = await admin.GetFromJsonAsync<JsonElement>("/api/admin/dashboard");
        Assert.Equal(4, dashboard.GetProperty("total").GetInt32());
        foreach (var status in new[] { "pending", "confirmed", "completed", "cancelled" })
            Assert.Equal(1, dashboard.GetProperty(status).GetInt32());
        Assert.Equal(3, dashboard.GetProperty("today").GetInt32());
        Assert.Equal("2030-01-07", dashboard.GetProperty("todayDate").GetString());
        Assert.Equal(4, dashboard.GetProperty("recentAppointments").GetArrayLength());
        var filtered = await admin.GetFromJsonAsync<JsonElement>(
            "/api/admin/appointments?date=2030-01-07&status=Confirmed&patient=demo");
        Assert.Equal(1, filtered.GetProperty("total").GetInt32());
        Assert.Equal("Confirmed", filtered.GetProperty("items")[0].GetProperty("status").GetString());
        var first = await admin.GetFromJsonAsync<JsonElement>("/api/admin/appointments?pageSize=2&page=1");
        var second = await admin.GetFromJsonAsync<JsonElement>("/api/admin/appointments?pageSize=2&page=2");
        Assert.Equal(2, first.GetProperty("items").GetArrayLength());
        Assert.Equal(2, second.GetProperty("items").GetArrayLength());
        Assert.NotEqual(first.GetProperty("items")[0].GetProperty("id").GetInt32(), second.GetProperty("items")[0].GetProperty("id").GetInt32());
        foreach (var query in new[] { "page=0", "pageSize=101", "date=bad", "status=7", "status=invalid" })
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"/api/admin/appointments?{query}")).StatusCode);
        var empty = await admin.GetFromJsonAsync<JsonElement>("/api/admin/appointments?patient=nonexistent");
        Assert.Equal(0, empty.GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData("Pending", "Pending", false)]
    [InlineData("Pending", "Confirmed", true)]
    [InlineData("Pending", "Completed", false)]
    [InlineData("Pending", "Cancelled", true)]
    [InlineData("Confirmed", "Pending", false)]
    [InlineData("Confirmed", "Confirmed", false)]
    [InlineData("Confirmed", "Completed", true)]
    [InlineData("Confirmed", "Cancelled", true)]
    [InlineData("Completed", "Pending", false)]
    [InlineData("Completed", "Confirmed", false)]
    [InlineData("Completed", "Completed", false)]
    [InlineData("Completed", "Cancelled", false)]
    [InlineData("Cancelled", "Pending", false)]
    [InlineData("Cancelled", "Confirmed", false)]
    [InlineData("Cancelled", "Completed", false)]
    [InlineData("Cancelled", "Cancelled", false)]
    public async Task Status_transitions_preserve_history_and_reservations(string from, string to, bool allowed)
    {
        using var factory = Factory();
        using var admin = await Admin(factory);
        using var patient = await Patient(factory);
        var id = await Seed(factory, patient, Enum.Parse<AppointmentStatus>(from));
        var response = await Send(admin, HttpMethod.Patch, $"/api/admin/appointments/{id}/status",
            new { status = to, expectedStatus = from });
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Conflict, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
        var item = await database.Appointments.Include(item => item.Reservation).SingleAsync();
        var actual = allowed ? to : from;
        Assert.Equal(actual, item.Status.ToString());
        Assert.Equal(actual is "Pending" or "Confirmed", item.Reservation is not null);
        if (allowed)
        {
            Assert.Equal(DateTime.Parse("2030-01-07T00:00:00Z").ToUniversalTime(), item.UpdatedAtUtc);
            Assert.Equal(to == "Cancelled", item.CancelledAtUtc.HasValue);
        }
    }

    [Fact]
    public async Task Stale_or_invalid_status_requests_do_not_change_the_appointment()
    {
        using var factory = Factory();
        using var admin = await Admin(factory);
        using var patient = await Patient(factory);
        var id = await Seed(factory, patient, AppointmentStatus.Confirmed);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(admin, HttpMethod.Patch, $"/api/admin/appointments/{id}/status",
            new { status = "Cancelled", expectedStatus = "Pending" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, HttpMethod.Patch, $"/api/admin/appointments/{id}/status",
            new { status = "999", expectedStatus = "Confirmed" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(admin, HttpMethod.Patch, "/api/admin/appointments/999/status",
            new { status = "Confirmed", expectedStatus = "Pending" })).StatusCode);
    }

    [Fact]
    public async Task Schedule_changes_reject_conflicts_and_accept_safe_hours()
    {
        using var factory = Factory();
        using var admin = await Admin(factory);
        using var patient = await Patient(factory);
        await Seed(factory, patient);
        foreach (var values in new object[]
        {
            new { isOpen = false, openingTime = "09:00", closingTime = "17:00" },
            new { isOpen = true, openingTime = "09:30", closingTime = "17:00" },
            new { isOpen = true, openingTime = "08:00", closingTime = "09:00" }
        })
            Assert.Equal(HttpStatusCode.Conflict, (await Send(admin, HttpMethod.Put, "/api/admin/schedules/2", values)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(admin, HttpMethod.Put, "/api/admin/schedules/2",
            new { isOpen = true, openingTime = "09:00", closingTime = "09:30" })).StatusCode);
        var slots = await patient.GetFromJsonAsync<JsonElement>("/api/availability?date=2030-01-07");
        Assert.Empty(slots.GetProperty("slots").EnumerateArray()); // Sole slot remains reserved.
        Assert.Equal(HttpStatusCode.OK, (await Send(admin, HttpMethod.Put, "/api/admin/schedules/3",
            new { isOpen = true, openingTime = "10:00", closingTime = "11:00" })).StatusCode);
        var next = await patient.GetFromJsonAsync<JsonElement>("/api/availability?date=2030-01-08");
        Assert.Equal(2, next.GetProperty("slots").GetArrayLength());
        Assert.Equal("10:00", next.GetProperty("slots")[0].GetProperty("localTime").GetString());
    }

    [Theory]
    [InlineData("09:15", "17:00")]
    [InlineData("17:00", "09:00")]
    [InlineData("09:00", "09:00")]
    [InlineData("bad", "17:00")]
    [InlineData("09:00", "24:00")]
    public async Task Invalid_hours_are_rejected(string openingTime, string closingTime)
    {
        using var factory = Factory();
        using var admin = await Admin(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, HttpMethod.Put, "/api/admin/schedules/2",
            new { isOpen = true, openingTime, closingTime })).StatusCode);
    }

    [Fact]
    public async Task Blocking_and_unblocking_dates_controls_availability_and_validates_conflicts()
    {
        using var factory = Factory();
        using var admin = await Admin(factory);
        using var patient = await Patient(factory);
        var id = await Seed(factory, patient);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(admin, HttpMethod.Post, "/api/admin/blocked-dates",
            new { date = "2030-01-07" })).StatusCode);
        await Send(admin, HttpMethod.Patch, $"/api/admin/appointments/{id}/status",
            new { status = "Cancelled", expectedStatus = "Pending" });
        var added = await Send(admin, HttpMethod.Post, "/api/admin/blocked-dates", new { date = "2030-01-07", reason = "Maintenance" });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var blockedId = (await added.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await Send(admin, HttpMethod.Post, "/api/admin/blocked-dates",
            new { date = "2030-01-07" })).StatusCode);
        var slots = await patient.GetFromJsonAsync<JsonElement>("/api/availability?date=2030-01-07");
        Assert.Empty(slots.GetProperty("slots").EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await Send(admin, HttpMethod.Delete, $"/api/admin/blocked-dates/{blockedId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(admin, HttpMethod.Delete, $"/api/admin/blocked-dates/{blockedId}")).StatusCode);
        var reopened = await patient.GetFromJsonAsync<JsonElement>("/api/availability?date=2030-01-07");
        Assert.Equal(16, reopened.GetProperty("slots").GetArrayLength());
        foreach (var date in new[] { "2030-01-06", "invalid" })
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(admin, HttpMethod.Post, "/api/admin/blocked-dates", new { date })).StatusCode);
    }

    [Fact]
    public async Task Ongoing_confirmed_visits_are_protected_from_schedule_and_date_changes()
    {
        var clock = new TestClock { Now = DateTimeOffset.Parse("2030-01-07T01:15:00Z") };
        using var factory = Factory(clock);
        using var admin = await Admin(factory);
        using var patient = await Patient(factory);
        await Seed(factory, patient, AppointmentStatus.Confirmed);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(admin, HttpMethod.Put, "/api/admin/schedules/2",
            new { isOpen = false, openingTime = "09:00", closingTime = "17:00" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(admin, HttpMethod.Post, "/api/admin/blocked-dates",
            new { date = "2030-01-07" })).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_booking_and_availability_change_cannot_both_invalidate_each_other(bool block)
    {
        using var factory = Factory();
        using var admin = await Admin(factory);
        using var patient = await Patient(factory);
        using var booking = await Request(patient, HttpMethod.Post, "/api/appointments",
            new { appointmentDateTimeUtc = "2030-01-07T01:00:00Z" });
        using var change = block
            ? await Request(admin, HttpMethod.Post, "/api/admin/blocked-dates", new { date = "2030-01-07" })
            : await Request(admin, HttpMethod.Put, "/api/admin/schedules/2",
                new { isOpen = false, openingTime = "09:00", closingTime = "17:00" });
        using var barrier = new Barrier(2);
        async Task<HttpResponseMessage> Compete(HttpClient client, HttpRequestMessage request)
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(15))) throw new TimeoutException();
            return await client.SendAsync(request);
        }
        var responses = await Task.WhenAll(Task.Run(() => Compete(patient, booking)), Task.Run(() => Compete(admin, change)));
        Assert.Single(responses, response => response.IsSuccessStatusCode);
        if (responses[0].IsSuccessStatusCode)
            Assert.Equal(HttpStatusCode.Conflict, responses[1].StatusCode);
        else Assert.Equal(HttpStatusCode.BadRequest, responses[0].StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppointlyDbContext>();
        Assert.Equal(responses[0].IsSuccessStatusCode ? 1 : 0, await database.Appointments.CountAsync());
        Assert.Equal(responses[0].IsSuccessStatusCode ? 1 : 0, await database.SlotReservations.CountAsync());
        if (block) Assert.Equal(responses[1].IsSuccessStatusCode ? 1 : 0, await database.BlockedDates.CountAsync());
        else Assert.Equal(!responses[1].IsSuccessStatusCode, (await database.ClinicSchedules.FindAsync(2))!.IsOpen);
    }

    [Fact]
    public async Task Every_admin_route_rejects_patients_and_mutations_require_csrf()
    {
        using var factory = Factory();
        using var patient = await Patient(factory);
        using var admin = await Admin(factory);
        using var anonymous = factory.Browser();
        var routes = new (HttpMethod Method, string Path)[]
        {
            (HttpMethod.Get, "/api/admin/dashboard"), (HttpMethod.Get, "/api/admin/appointments"),
            (HttpMethod.Get, "/api/admin/schedules"), (HttpMethod.Get, "/api/admin/blocked-dates"),
            (HttpMethod.Patch, "/api/admin/appointments/1/status"), (HttpMethod.Put, "/api/admin/schedules/2"),
            (HttpMethod.Post, "/api/admin/blocked-dates"), (HttpMethod.Delete, "/api/admin/blocked-dates/1")
        };
        foreach (var (method, path) in routes)
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await patient.SendAsync(new HttpRequestMessage(method, path))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.SendAsync(new HttpRequestMessage(method, path))).StatusCode);
            if (method != HttpMethod.Get)
                Assert.Equal(HttpStatusCode.BadRequest, (await admin.SendAsync(new HttpRequestMessage(method, path))).StatusCode);
        }
    }
}
