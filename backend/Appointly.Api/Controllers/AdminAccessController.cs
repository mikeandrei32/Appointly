using Appointly.Api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Appointly.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/access")]
public sealed class AdminAccessController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { message = "Administrator access verified." });
}
