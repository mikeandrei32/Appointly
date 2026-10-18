using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Appointly.Api.Configuration;
using Appointly.Api.Data;
using Appointly.Api.Models;
using Appointly.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Appointly.Tests;

public sealed class AuthenticationTests
{
    private static string Password() => $"Aa1!{Guid.NewGuid():N}";
    private static object Registration(string email, string password) => new
    {
        fullName = "Demo Patient", email, password, confirmPassword = password
    };
    private static async Task<string> Csrf(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        return result.GetProperty("token").GetString()!;
    }
    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", await Csrf(client));
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Registration_login_profile_and_logout_work_with_secure_cookies()
    {
        using var factory = new AuthFactory();
        using var client = factory.Browser();
        var password = Password();
        const string email = "patient@example.test";
        Assert.Equal(HttpStatusCode.Created, (await Post(client, "/api/auth/register", Registration(email, password))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        var anonymousToken = await Csrf(client);
        var login = await Post(client, "/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith("Appointly.Auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        var profile = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal(email, profile.GetProperty("email").GetString());
        Assert.Equal("Patient", Assert.Single(profile.GetProperty("roles").EnumerateArray()).GetString());
        Assert.False(profile.TryGetProperty("passwordHash", out _));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/access")).StatusCode);
        using var stale = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        stale.Headers.Add("X-CSRF-TOKEN", anonymousToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(stale)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post(client, "/api/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email);
        Assert.NotNull(user?.PasswordHash);
        Assert.NotEqual(password, user.PasswordHash);
    }

    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/admin/access")]
    public async Task Anonymous_requests_receive_401_without_redirect(string path)
    {
        using var factory = new AuthFactory();
        using var client = factory.Browser();
        var result = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Null(result.Headers.Location);
    }

    [Fact]
    public async Task Missing_csrf_and_client_supplied_roles_are_rejected()
    {
        using var factory = new AuthFactory();
        using var client = factory.Browser();
        var password = Password();
        foreach (var path in new[] { "/api/auth/register", "/api/auth/login" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, Registration("test@example.test", password))).StatusCode);
        var attempt = await Post(client, "/api/auth/register", new
        {
            fullName = "Demo Patient", email = "test@example.test", password, confirmPassword = password, role = "Admin"
        });
        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppointlyDbContext>().Users.ToListAsync());
    }

    [Fact]
    public async Task Validation_duplicate_emails_and_incorrect_credentials_are_rejected()
    {
        using var factory = new AuthFactory();
        using var client = factory.Browser();
        var password = Password();
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/auth/register", Registration("invalid", password))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/auth/register", Registration("test@example.test", "weak"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/auth/register", new
        { fullName = "Demo Patient", email = "test@example.test", password, confirmPassword = Password() })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Post(client, "/api/auth/register", Registration("test@example.test", password))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "/api/auth/register", Registration("TEST@example.test", password))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "/api/auth/login", new { email = "test@example.test", password = Password() })).StatusCode);
    }

    [Fact]
    public async Task Five_failed_passwords_lock_the_account()
    {
        using var factory = new AuthFactory();
        using var client = factory.Browser();
        var password = Password();
        const string email = "locked@example.test";
        await Post(client, "/api/auth/register", Registration(email, password));
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "/api/auth/login", new { email, password = Password() })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "/api/auth/login", new { email, password })).StatusCode);
    }

    [Fact]
    public async Task Authentication_requests_are_rate_limited()
    {
        using var factory = new AuthFactory();
        using var client = factory.Browser();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/login", new { })).StatusCode);
    }

    [Fact]
    public async Task Development_admin_can_login_and_access_admin_endpoint()
    {
        using var factory = new AuthFactory("Development", seedAdmin: true);
        using var client = factory.Browser();
        Assert.Equal(HttpStatusCode.OK, (await Post(client, "/api/auth/login", new { email = factory.AdminEmail, password = factory.AdminPassword })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/access")).StatusCode);
        var profile = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal(AppRoles.Admin, Assert.Single(profile.GetProperty("roles").EnumerateArray()).GetString());
        await IdentitySeeder.SeedAsync(factory.Services, factory.Services.GetRequiredService<IConfiguration>(),
            factory.Services.GetRequiredService<IHostEnvironment>());
        using var scope = factory.Services.CreateScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<AppointlyDbContext>().Users.ToListAsync());
    }

    [Fact]
    public async Task Development_seeder_never_promotes_an_existing_patient()
    {
        using var factory = new AuthFactory("Development");
        using var client = factory.Browser();
        var password = Password();
        const string email = "patient@example.test";
        Assert.Equal(HttpStatusCode.Created, (await Post(client, "/api/auth/register", Registration(email, password))).StatusCode);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DevelopmentAdmin:Enabled"] = "true", ["DevelopmentAdmin:Email"] = email,
            ["DevelopmentAdmin:Password"] = password
        }).Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => IdentitySeeder.SeedAsync(factory.Services,
            configuration, factory.Services.GetRequiredService<IHostEnvironment>()));
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.False(await users.IsInRoleAsync(user!, AppRoles.Admin));
    }

    [Fact]
    public async Task Production_never_seeds_development_admin_or_exposes_openapi()
    {
        using var factory = new AuthFactory("Production", seedAdmin: true);
        using var client = factory.Browser();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppointlyDbContext>().Users.ToListAsync());
    }
}
