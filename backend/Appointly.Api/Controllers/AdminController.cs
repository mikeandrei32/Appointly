using Appointly.Api.Configuration;
using Appointly.Api.DTOs;
using Appointly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appointly.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminController(AdminService admin) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken token) => Ok(await admin.DashboardAsync(token));

    [HttpGet("appointments")]
    public async Task<IActionResult> Appointments([FromQuery] AdminAppointmentQuery query, CancellationToken token) =>
        Ok(await admin.AppointmentsAsync(query, token));

    [HttpPatch("appointments/{id:int}/status")]
    public async Task<IActionResult> Status(int id, ChangeStatusRequest request, CancellationToken token) =>
        Ok(await admin.ChangeStatusAsync(id, request, token));

    [HttpGet("schedules")]
    public async Task<IActionResult> Schedules(CancellationToken token) => Ok(await admin.SchedulesAsync(token));

    [HttpPut("schedules/{id:int}")]
    public async Task<IActionResult> Schedule(int id, UpdateScheduleRequest request, CancellationToken token) =>
        Ok(await admin.UpdateScheduleAsync(id, request, token));

    [HttpGet("blocked-dates")]
    public async Task<IActionResult> BlockedDates(CancellationToken token) => Ok(await admin.BlockedDatesAsync(token));

    [HttpPost("blocked-dates")]
    public async Task<IActionResult> BlockDate(BlockDateRequest request, CancellationToken token)
    {
        var item = await admin.BlockDateAsync(request, token);
        return StatusCode(201, item);
    }

    [HttpDelete("blocked-dates/{id:int}")]
    public async Task<IActionResult> Unblock(int id, CancellationToken token)
    {
        await admin.RemoveBlockedDateAsync(id, token);
        return NoContent();
    }
}
