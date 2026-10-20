using Appointly.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Appointly.Api.Data;

public sealed class AppointlyDbContext(DbContextOptions<AppointlyDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ClinicSchedule> ClinicSchedules => Set<ClinicSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<SlotReservation> SlotReservations => Set<SlotReservation>();
    public DbSet<BlockedDate> BlockedDates => Set<BlockedDate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>().Property(user => user.FullName).HasMaxLength(100);
        var appointment = modelBuilder.Entity<Appointment>();
        appointment.HasOne(item => item.Patient).WithMany().HasForeignKey(item => item.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        appointment.Property(item => item.ReasonForVisit).HasMaxLength(200);
        appointment.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        appointment.HasIndex(item => new { item.PatientId, item.AppointmentDateTimeUtc });
        appointment.Property(item => item.AppointmentDateTimeUtc).HasConversion(
            value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        appointment.Property(item => item.CreatedAtUtc).HasConversion(
            value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        appointment.Property(item => item.UpdatedAtUtc).HasConversion(
            value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        appointment.Property(item => item.CancelledAtUtc).HasConversion(
            value => value, value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : (DateTime?)null);
        appointment.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Appointment_Duration", "\"DurationMinutes\" = 30");
            table.HasCheckConstraint("CK_Appointment_Status", "\"Status\" IN ('Pending', 'Confirmed', 'Completed', 'Cancelled')");
        });
        var reservation = modelBuilder.Entity<SlotReservation>();
        // A primary key is database-enforced: two requests cannot own the same UTC slot.
        reservation.HasKey(item => item.SlotStartUtc);
        reservation.HasOne(item => item.Appointment).WithOne(item => item.Reservation)
            .HasForeignKey<SlotReservation>(item => item.AppointmentId);
        reservation.Property(item => item.SlotStartUtc).HasConversion(
            value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        modelBuilder.Entity<BlockedDate>().HasIndex(item => item.Date).IsUnique();
        modelBuilder.Entity<BlockedDate>().Property(item => item.Reason).HasMaxLength(200);
        modelBuilder.Entity<ClinicSchedule>().HasIndex(schedule => schedule.DayOfWeek).IsUnique();
        modelBuilder.Entity<ClinicSchedule>().HasData(
            Enum.GetValues<DayOfWeek>().Select(day => new ClinicSchedule
            {
                Id = (int)day + 1,
                DayOfWeek = day,
                OpeningTime = new TimeOnly(9, 0),
                ClosingTime = new TimeOnly(17, 0),
                IsOpen = day is not DayOfWeek.Saturday and not DayOfWeek.Sunday
            }));
    }
}
