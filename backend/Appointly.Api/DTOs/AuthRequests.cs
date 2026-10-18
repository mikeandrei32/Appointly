using System.ComponentModel.DataAnnotations;

namespace Appointly.Api.DTOs;

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = string.Empty;
    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;
    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}

public sealed record UserResponse(string Id, string FullName, string Email, IList<string> Roles);
