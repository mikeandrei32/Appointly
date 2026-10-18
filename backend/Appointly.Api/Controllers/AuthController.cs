using Appointly.Api.Configuration;
using Appointly.Api.Data;
using Appointly.Api.DTOs;
using Appointly.Api.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Appointly.Api.Controllers;

[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, AppointlyDbContext database,
    IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    public IActionResult Csrf() => Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("register")]
    [EnableRateLimiting("authentication")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (request.FullName.Trim().Length < 2)
            return Problem(statusCode: 400, title: "Enter a full name of at least two characters.");
        var email = request.Email.Trim();
        var user = new ApplicationUser { UserName = email, Email = email, FullName = request.FullName.Trim() };
        await using var transaction = await database.Database.BeginTransactionAsync();
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Code.StartsWith("Duplicate", StringComparison.Ordinal)
                ? "Unable to register with these details. Try signing in instead."
                : error.Description).Distinct().ToArray();
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["registration"] = errors }));
        }
        var roleResult = await users.AddToRoleAsync(user, AppRoles.Patient);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException("Unable to assign the patient role.");
        await transaction.CommitAsync();
        // Registration deliberately does not sign in; the UI sends the patient to login.
        return StatusCode(StatusCodes.Status201Created, new { message = "Account created. You can now sign in." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("authentication")]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var result = await signIn.PasswordSignInAsync(request.Email.Trim(), request.Password,
            isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Problem(statusCode: 401, title: "Unable to sign in.",
                detail: "Check your email and password, or try again later.");
        var user = await users.FindByEmailAsync(request.Email.Trim());
        return Ok(await ToResponse(user!));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var user = await users.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await ToResponse(user));
    }

    private async Task<UserResponse> ToResponse(ApplicationUser user) =>
        new(user.Id, user.FullName, user.Email!, await users.GetRolesAsync(user));
}
