using System.Globalization;
using Appointly.Api.Configuration;
using Appointly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appointly.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Patient)]
[Route("api/availability")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AvailabilityController(BookingService bookings) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string date, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var day) || day.Year < 2000 || day.Year > 2100)
            return Problem(statusCode: 400, title: "Use a date between 2000 and 2100 in YYYY-MM-DD format.");
        return Ok(await bookings.GetAvailabilityAsync(day, cancellationToken));
    }
}
