namespace Appointly.Api.Models;

public enum AppointmentStatus { Pending, Confirmed, Completed, Cancelled }

public sealed class Appointment
{
    public int Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public ApplicationUser Patient { get; set; } = null!;
    public DateTime AppointmentDateTimeUtc { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? ReasonForVisit { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public SlotReservation? Reservation { get; set; }
}
