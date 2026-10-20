using System.ComponentModel.DataAnnotations;

namespace Appointly.Api.DTOs;

public sealed class BookAppointmentRequest
{
    [Required, StringLength(40)]
    public string AppointmentDateTimeUtc { get; init; } = string.Empty;

    [StringLength(200)]
    public string? ReasonForVisit { get; init; }
}

public sealed record AppointmentResponse(int Id, DateTime AppointmentDateTimeUtc,
    int DurationMinutes, string Status, string? ReasonForVisit, DateTime CreatedAtUtc,
    DateTime? CancelledAtUtc, bool CanCancel);

public sealed record AvailableSlot(DateTime StartUtc, string LocalTime);
public sealed record PatientOverviewResponse(
    IReadOnlyList<AppointmentResponse> Upcoming,
    IReadOnlyList<AppointmentResponse> History);
public sealed record AvailabilityResponse(string Date, string Timezone, int DurationMinutes,
    IReadOnlyList<AvailableSlot> Slots);
