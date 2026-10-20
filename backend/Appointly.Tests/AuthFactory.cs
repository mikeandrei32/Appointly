using Appointly.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Appointly.Tests;

public sealed class AuthFactory(string environment = "Testing", bool seedAdmin = false,
    TimeProvider? timeProvider = null) : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"appointly-{Guid.NewGuid():N}.db");
    public string AdminEmail { get; } = $"admin-{Guid.NewGuid():N}@example.test";
    public string AdminPassword { get; } = $"Aa1!{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["DevelopmentAdmin:Enabled"] = seedAdmin.ToString(),
                ["DevelopmentAdmin:Email"] = AdminEmail,
                ["DevelopmentAdmin:Password"] = AdminPassword
            }));
        builder.ConfigureServices(services =>
        {
            if (timeProvider is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(timeProvider);
                // Booking tests move clinic time; keep the test client's cookie alive on real time.
                services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme,
                    options => options.TimeProvider = TimeProvider.System);
            }
            services.RemoveAll<DbContextOptions<AppointlyDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppointlyDbContext>>();
            var connection = $"Data Source={databasePath};Pooling=False";
            services.AddDbContext<AppointlyDbContext>(options => options.UseSqlite(connection));
            using var database = new AppointlyDbContext(new DbContextOptionsBuilder<AppointlyDbContext>()
                .UseSqlite(connection).Options);
            database.Database.Migrate();
        });
    }

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true
    });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                File.Delete(databasePath + suffix);
    }
}
