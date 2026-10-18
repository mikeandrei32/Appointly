using Appointly.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Appointly.Api.Data;

public sealed class AppointlyDbContext(DbContextOptions<AppointlyDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ClinicSchedule> ClinicSchedules => Set<ClinicSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>().Property(user => user.FullName).HasMaxLength(100);
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
