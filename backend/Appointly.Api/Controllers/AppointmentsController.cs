using System.Security.Claims;
using Appointly.Api.Configuration;
using Appointly.Api.DTOs;
using Appointly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appointly.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Patient)]
[Route("api/appointments")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AppointmentsController(BookingService bookings) : ControllerBase
{
    private string PatientId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("my")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken) =>
        Ok(await bookings.GetMineAsync(PatientId, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        Ok(await bookings.GetAsync(id, PatientId, cancellationToken));

    [HttpGet("my/overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken) =>
        Ok(await bookings.GetOverviewAsync(PatientId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Book(BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        var appointment = await bookings.BookAsync(PatientId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = appointment.Id }, appointment);
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken) =>
        Ok(await bookings.CancelAsync(id, PatientId, cancellationToken));
}
