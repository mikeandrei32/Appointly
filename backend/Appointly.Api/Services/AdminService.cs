using System.Globalization;
using Appointly.Api.Data;
using Appointly.Api.DTOs;
using Appointly.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Appointly.Api.Services;

public sealed class AdminService(AppointlyDbContext database, ScheduleWriter writer, TimeProvider clock)
{
    public async Task<AdminDashboardResponse> DashboardAsync(CancellationToken cancellationToken)
    {
        var counts = await database.Appointments.AsNoTracking().GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var today = DateOnly.FromDateTime(ClinicTime.ToLocal(clock.GetUtcNow().UtcDateTime));
        var start = ClinicTime.ToUtc(today, TimeOnly.MinValue);
        var end = start.AddDays(1);
        var todayCount = await database.Appointments.CountAsync(
            item => item.AppointmentDateTimeUtc >= start && item.AppointmentDateTimeUtc < end, cancellationToken);
        var recent = await database.Appointments.AsNoTracking().Include(item => item.Patient)
            .OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
            .Take(8).ToListAsync(cancellationToken);
        return new(counts.Values.Sum(), counts.GetValueOrDefault(AppointmentStatus.Pending),
            counts.GetValueOrDefault(AppointmentStatus.Confirmed), counts.GetValueOrDefault(AppointmentStatus.Completed),
            counts.GetValueOrDefault(AppointmentStatus.Cancelled), todayCount, DateText(today),
            recent.Select(ToResponse).ToList());
    }

    public async Task<AdminAppointmentPage> AppointmentsAsync(AdminAppointmentQuery filter, CancellationToken cancellationToken)
    {
        var query = database.Appointments.AsNoTracking().Include(item => item.Patient).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Date))
        {
            var start = ClinicTime.ToUtc(ParseDate(filter.Date), TimeOnly.MinValue);
            var end = start.AddDays(1);
            query = query.Where(item => item.AppointmentDateTimeUtc >= start && item.AppointmentDateTimeUtc < end);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var status = ParseStatus(filter.Status);
            query = query.Where(item => item.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(filter.Patient))
        {
            var term = filter.Patient.Trim().ToLowerInvariant();
            query = query.Where(item => item.Patient.FullName.ToLower().Contains(term) ||
                item.Patient.Email!.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.AppointmentDateTimeUtc).ThenByDescending(item => item.Id)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);
        return new(items.Select(ToResponse).ToList(), total, filter.Page, filter.PageSize);
    }

    public async Task<AdminAppointmentResponse> ChangeStatusAsync(int id, ChangeStatusRequest request,
        CancellationToken cancellationToken)
    {
        var next = ParseStatus(request.Status);
        var expected = ParseStatus(request.ExpectedStatus);
        return await writer.WriteAsync(async () =>
        {
            var item = await database.Appointments.Include(item => item.Patient).Include(item => item.Reservation)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new BookingException(404, "Appointment not found.");
            if (item.Status != expected)
                throw new BookingException(409, "This appointment changed. Refresh before choosing an action.");
            var allowed = item.Status switch
            {
                AppointmentStatus.Pending => next is AppointmentStatus.Confirmed or AppointmentStatus.Cancelled,
                AppointmentStatus.Confirmed => next is AppointmentStatus.Completed or AppointmentStatus.Cancelled,
                _ => false
            };
            if (!allowed) throw new BookingException(409, "This status transition is not allowed.");
            item.Status = next;
            item.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
            if (next == AppointmentStatus.Cancelled) item.CancelledAtUtc = item.UpdatedAtUtc;
            if ((next is AppointmentStatus.Cancelled or AppointmentStatus.Completed) && item.Reservation is not null)
                database.SlotReservations.Remove(item.Reservation);
            await database.SaveChangesAsync(cancellationToken);
            return ToResponse(item);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleResponse>> SchedulesAsync(CancellationToken cancellationToken) =>
        (await database.ClinicSchedules.AsNoTracking().OrderBy(item => item.DayOfWeek)
            .ToListAsync(cancellationToken)).Select(ToSchedule).ToList();

    public async Task<ScheduleResponse> UpdateScheduleAsync(int id, UpdateScheduleRequest request, CancellationToken cancellationToken)
    {
        if (!TimeOnly.TryParseExact(request.OpeningTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var opening) ||
            !TimeOnly.TryParseExact(request.ClosingTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var closing) ||
            !ClinicTime.IsGridAligned(opening) || !ClinicTime.IsGridAligned(closing) || closing <= opening)
            throw new BookingException(400, "Hours must use HH:mm, align to 30 minutes, and close after opening on the same day.");

        return await writer.WriteAsync(async () =>
        {
            var schedule = await database.ClinicSchedules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new BookingException(404, "Schedule not found.");
            var active = await ActiveFutureAppointments().ToListAsync(cancellationToken);
            var conflict = active.Any(item =>
            {
                var local = ClinicTime.ToLocal(item.AppointmentDateTimeUtc);
                if (local.DayOfWeek != schedule.DayOfWeek) return false;
                var time = TimeOnly.FromDateTime(local);
                return !request.IsOpen || time < opening ||
                    time.ToTimeSpan().Add(TimeSpan.FromMinutes(item.DurationMinutes)) > closing.ToTimeSpan();
            });
            if (conflict)
                throw new BookingException(409, "These hours would invalidate an active appointment. Manage the conflicting bookings first.");
            schedule.IsOpen = request.IsOpen;
            schedule.OpeningTime = opening;
            schedule.ClosingTime = closing;
            await database.SaveChangesAsync(cancellationToken);
            return ToSchedule(schedule);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<BlockedDateResponse>> BlockedDatesAsync(CancellationToken cancellationToken) =>
        (await database.BlockedDates.AsNoTracking().OrderBy(item => item.Date)
            .ToListAsync(cancellationToken)).Select(ToBlockedDate).ToList();

    public async Task<BlockedDateResponse> BlockDateAsync(BlockDateRequest request, CancellationToken cancellationToken)
    {
        var date = ParseDate(request.Date);
        return await writer.WriteAsync(async () =>
        {
            if (date < DateOnly.FromDateTime(ClinicTime.ToLocal(clock.GetUtcNow().UtcDateTime)))
                throw new BookingException(400, "Choose today or a future clinic date.");
            if (await database.BlockedDates.AnyAsync(item => item.Date == date, cancellationToken))
                throw new BookingException(409, "This date is already blocked.");
            var start = ClinicTime.ToUtc(date, TimeOnly.MinValue);
            var end = start.AddDays(1);
            if (await ActiveFutureAppointments().AnyAsync(
                    item => item.AppointmentDateTimeUtc >= start && item.AppointmentDateTimeUtc < end, cancellationToken))
                throw new BookingException(409, "This date has active appointments. Manage the conflicting bookings first.");
            var blocked = new BlockedDate { Date = date, Reason = request.Reason?.Trim() };
            database.BlockedDates.Add(blocked);
            await database.SaveChangesAsync(cancellationToken);
            return ToBlockedDate(blocked);
        }, cancellationToken);
    }

    public async Task RemoveBlockedDateAsync(int id, CancellationToken cancellationToken)
    {
        await writer.WriteAsync(async () =>
        {
            var item = await database.BlockedDates.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new BookingException(404, "Blocked date not found.");
            database.BlockedDates.Remove(item);
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    // Also protect ongoing visits, not just those that have yet to start.
    private IQueryable<Appointment> ActiveFutureAppointments()
    {
        var earliest = clock.GetUtcNow().UtcDateTime.AddMinutes(-ClinicTime.DurationMinutes);
        return database.Appointments.Where(item =>
            (item.Status == AppointmentStatus.Pending || item.Status == AppointmentStatus.Confirmed) &&
            item.AppointmentDateTimeUtc > earliest);
    }

    private static DateOnly ParseDate(string value)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            date.Year < 2000 || date.Year > 2100)
            throw new BookingException(400, "Use a date between 2000 and 2100 in YYYY-MM-DD format.");
        return date;
    }

    private static AppointmentStatus ParseStatus(string value)
    {
        if (!Enum.TryParse<AppointmentStatus>(value, out var status) || !Enum.IsDefined(status) || value != status.ToString())
            throw new BookingException(400, "Choose Pending, Confirmed, Completed, or Cancelled.");
        return status;
    }

    private static string DateText(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static AdminAppointmentResponse ToResponse(Appointment item) => new(item.Id,
        item.Patient.FullName, item.Patient.Email!, item.AppointmentDateTimeUtc,
        item.DurationMinutes, item.Status.ToString(), item.ReasonForVisit);
    private static ScheduleResponse ToSchedule(ClinicSchedule item) => new(item.Id, item.DayOfWeek.ToString(),
        item.IsOpen, item.OpeningTime.ToString("HH:mm", CultureInfo.InvariantCulture),
        item.ClosingTime.ToString("HH:mm", CultureInfo.InvariantCulture));
    private static BlockedDateResponse ToBlockedDate(BlockedDate item) => new(item.Id, DateText(item.Date), item.Reason);
}
