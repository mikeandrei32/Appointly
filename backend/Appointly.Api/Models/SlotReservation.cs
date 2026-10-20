namespace Appointly.Api.Models;

public sealed class SlotReservation
{
    public DateTime SlotStartUtc { get; set; }
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
}
