using System.ComponentModel.DataAnnotations;

namespace Appointly.Api.DTOs;

public sealed class AdminAppointmentQuery
{
    public string? Date { get; init; }
    [StringLength(100)] public string? Patient { get; init; }
    public string? Status { get; init; }
    [Range(1, 100000)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 20;
}

public sealed class ChangeStatusRequest
{
    [Required] public string Status { get; init; } = string.Empty;
    [Required] public string ExpectedStatus { get; init; } = string.Empty;
}

public sealed class UpdateScheduleRequest
{
    public bool IsOpen { get; init; }
    [Required] public string OpeningTime { get; init; } = string.Empty;
    [Required] public string ClosingTime { get; init; } = string.Empty;
}

public sealed class BlockDateRequest
{
    [Required] public string Date { get; init; } = string.Empty;
    [StringLength(200)] public string? Reason { get; init; }
}

public sealed record AdminAppointmentResponse(int Id, string PatientName, string PatientEmail,
    DateTime AppointmentDateTimeUtc, int DurationMinutes, string Status, string? ReasonForVisit);
public sealed record AdminAppointmentPage(IReadOnlyList<AdminAppointmentResponse> Items, int Total, int Page, int PageSize);
public sealed record AdminDashboardResponse(int Total, int Pending, int Confirmed, int Completed,
    int Cancelled, int Today, string TodayDate, IReadOnlyList<AdminAppointmentResponse> RecentAppointments);
public sealed record ScheduleResponse(int Id, string DayOfWeek, bool IsOpen, string OpeningTime, string ClosingTime);
public sealed record BlockedDateResponse(int Id, string Date, string? Reason);
