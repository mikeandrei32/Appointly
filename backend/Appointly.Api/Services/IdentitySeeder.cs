using System.ComponentModel.DataAnnotations;
using Appointly.Api.Configuration;
using Appointly.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Appointly.Api.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in new[] { AppRoles.Patient, AppRoles.Admin })
            if (!await roles.RoleExistsAsync(name))
                EnsureSuccess(await roles.CreateAsync(new IdentityRole(name)));

        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DevelopmentAdmin:Enabled"))
            return;

        var email = configuration["DevelopmentAdmin:Email"]?.Trim();
        var password = configuration["DevelopmentAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 ||
            !new EmailAddressAttribute().IsValid(email) || string.IsNullOrEmpty(password) ||
            password.Length > 128)
            throw new InvalidOperationException("Development admin requires a valid email and password in local configuration.");

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        if (user is not null)
        {
            // Never turn an existing public patient account into an administrator.
            if (!await users.IsInRoleAsync(user, AppRoles.Admin))
                throw new InvalidOperationException("Development admin email belongs to a non-admin account. Use a different email.");
            return;
        }

        var database = scope.ServiceProvider.GetRequiredService<Data.AppointlyDbContext>();
        await using var transaction = await database.Database.BeginTransactionAsync();
        user = new ApplicationUser { UserName = email, Email = email, FullName = "Development Administrator" };
        EnsureSuccess(await users.CreateAsync(user, password));
        EnsureSuccess(await users.AddToRoleAsync(user, AppRoles.Admin));
        await transaction.CommitAsync();
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Identity setup failed. Check configuration and password requirements.");
    }
}
