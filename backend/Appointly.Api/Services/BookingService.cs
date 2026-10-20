using System.Globalization;
using System.Text.RegularExpressions;
using Appointly.Api.Data;
using Appointly.Api.DTOs;
using Appointly.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Api.Services;

public sealed class BookingService(AppointlyDbContext database, TimeProvider clock, ScheduleWriter writer)
{
    public async Task<AvailabilityResponse> GetAvailabilityAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var slots = new List<AvailableSlot>();
        var now = clock.GetUtcNow().UtcDateTime;
        var schedule = await database.ClinicSchedules.AsNoTracking()
            .SingleOrDefaultAsync(item => item.DayOfWeek == date.DayOfWeek, cancellationToken);
        if (schedule is not { IsOpen: true } ||
            await database.BlockedDates.AnyAsync(item => item.Date == date, cancellationToken))
            return Availability(date, slots);

        var startOfDay = ClinicTime.ToUtc(date, TimeOnly.MinValue);
        var endOfDay = startOfDay.AddDays(1);
        var reserved = (await database.SlotReservations.AsNoTracking()
            .Where(item => item.SlotStartUtc >= startOfDay && item.SlotStartUtc < endOfDay)
            .Select(item => item.SlotStartUtc).ToListAsync(cancellationToken)).ToHashSet();

        // Integer minutes avoid TimeOnly wrapping around midnight.
        for (var minute = 0; minute <= 24 * 60 - ClinicTime.DurationMinutes; minute += ClinicTime.DurationMinutes)
        {
            var time = TimeOnly.MinValue.AddMinutes(minute);
            if (!FitsSchedule(schedule, time)) continue;
            var utc = ClinicTime.ToUtc(date, time);
            if (utc > now && !reserved.Contains(utc))
                slots.Add(new AvailableSlot(utc, time.ToString("HH:mm", CultureInfo.InvariantCulture)));
        }
        return Availability(date, slots);
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetMineAsync(string patientId, CancellationToken cancellationToken)
    {
        var items = await database.Appointments.AsNoTracking().Where(item => item.PatientId == patientId)
            .OrderByDescending(item => item.AppointmentDateTimeUtc).ToListAsync(cancellationToken);
        return items.Select(ToResponse).ToList();
    }

    public async Task<PatientOverviewResponse> GetOverviewAsync(string patientId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        // Always scope to the signed-in patient before loading any records.
        var items = await database.Appointments.AsNoTracking()
            .Where(item => item.PatientId == patientId).ToListAsync(cancellationToken);
        var upcoming = new List<AppointmentResponse>();
        var history = new List<AppointmentResponse>();
        foreach (var item in items)
        {
            var response = ToResponse(item, now);
            // Include ongoing visits until their end, without inventing a new status.
            if (IsActive(item.Status) && item.AppointmentDateTimeUtc.AddMinutes(item.DurationMinutes) > now)
                upcoming.Add(response);
            else
                history.Add(response);
        }
        return new(
            upcoming.OrderBy(item => item.AppointmentDateTimeUtc).ThenBy(item => item.Id).ToList(),
            history.OrderByDescending(item => item.AppointmentDateTimeUtc).ThenByDescending(item => item.Id).ToList());
    }

    public async Task<AppointmentResponse> GetAsync(int id, string patientId, CancellationToken cancellationToken)
    {
        var item = await database.Appointments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.PatientId == patientId, cancellationToken)
            ?? throw new BookingException(404, "Appointment not found.");
        return ToResponse(item);
    }

    public async Task<AppointmentResponse> BookAsync(string patientId, BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!Regex.IsMatch(request.AppointmentDateTimeUtc, @"(Z|[+-]\d{2}:\d{2})$") ||
            !DateTimeOffset.TryParse(request.AppointmentDateTimeUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var timestamp) || timestamp.Year < 2000 || timestamp.Year > 2100)
            throw new BookingException(400, "Choose a valid appointment timestamp with an explicit timezone.");
        var start = timestamp.UtcDateTime;
        var local = ClinicTime.ToLocal(start);
        var date = DateOnly.FromDateTime(local);
        var time = TimeOnly.FromDateTime(local);

        return await writer.WriteAsync(async () =>
        {
            var now = clock.GetUtcNow().UtcDateTime;
            if (start <= now) throw new BookingException(400, "Appointments must be in the future.");
            var schedule = await database.ClinicSchedules.SingleOrDefaultAsync(
                item => item.DayOfWeek == date.DayOfWeek, cancellationToken);
            if (!ClinicTime.IsGridAligned(time) || schedule is null || !FitsSchedule(schedule, time))
                throw new BookingException(400, "Choose a 30-minute slot within clinic operating hours.");
            if (await database.BlockedDates.AnyAsync(item => item.Date == date, cancellationToken))
                throw new BookingException(400, "The clinic is unavailable on this date.");
            if (await database.SlotReservations.AnyAsync(item => item.SlotStartUtc == start, cancellationToken))
                throw new BookingException(409, "This slot was just reserved. Choose another time.");

            var appointment = new Appointment
            {
                PatientId = patientId, AppointmentDateTimeUtc = start,
                ReasonForVisit = string.IsNullOrWhiteSpace(request.ReasonForVisit) ? null : request.ReasonForVisit.Trim(),
                CreatedAtUtc = now, UpdatedAtUtc = now,
                Reservation = new SlotReservation { SlotStartUtc = start }
            };
            database.Appointments.Add(appointment);
            await database.SaveChangesAsync(cancellationToken);
            return ToResponse(appointment);
        }, cancellationToken);
    }

    public async Task<AppointmentResponse> CancelAsync(int id, string patientId, CancellationToken cancellationToken)
    {
        return await writer.WriteAsync(async () =>
        {
            var appointment = await database.Appointments.Include(item => item.Reservation)
                .SingleOrDefaultAsync(item => item.Id == id && item.PatientId == patientId, cancellationToken)
                ?? throw new BookingException(404, "Appointment not found.");
            var now = clock.GetUtcNow().UtcDateTime;
            if (!IsActive(appointment.Status) || appointment.AppointmentDateTimeUtc <= now)
                throw new BookingException(409, "Only future Pending or Confirmed appointments can be cancelled.");
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancelledAtUtc = now;
            appointment.UpdatedAtUtc = now;
            if (appointment.Reservation is not null) database.SlotReservations.Remove(appointment.Reservation);
            await database.SaveChangesAsync(cancellationToken);
            return ToResponse(appointment);
        }, cancellationToken);
    }

    private static bool FitsSchedule(ClinicSchedule schedule, TimeOnly time) =>
        schedule.IsOpen && time >= schedule.OpeningTime &&
        time.ToTimeSpan().Add(TimeSpan.FromMinutes(ClinicTime.DurationMinutes)) <= schedule.ClosingTime.ToTimeSpan();

    private static bool IsActive(AppointmentStatus status) =>
        status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;

    private AppointmentResponse ToResponse(Appointment item) => ToResponse(item, clock.GetUtcNow().UtcDateTime);

    private static AppointmentResponse ToResponse(Appointment item, DateTime now) => new(item.Id,
        item.AppointmentDateTimeUtc, item.DurationMinutes, item.Status.ToString(), item.ReasonForVisit,
        item.CreatedAtUtc, item.CancelledAtUtc,
        IsActive(item.Status) && item.AppointmentDateTimeUtc > now);

    private static AvailabilityResponse Availability(DateOnly date, List<AvailableSlot> slots) =>
        new(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ClinicTime.TimezoneId,
            ClinicTime.DurationMinutes, slots);
}
