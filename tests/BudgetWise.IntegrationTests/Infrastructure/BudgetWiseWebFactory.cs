using BudgetWise.Infrastructure.Persistence;
using HealthChecks.NpgSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Testcontainers.PostgreSql;

namespace BudgetWise.IntegrationTests.Infrastructure;

public sealed class BudgetWiseWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Jwt:Secret"] = "test-secret-budgetwise-at-least-32-chars!",
                ["Jwt:Issuer"] = "budgetwise-api",
                ["Jwt:Audience"] = "budgetwise-client",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Seq:ServerUrl"] = "",
                ["RateLimit:Auth:LoginPermitLimit"] = "10000",
                ["RateLimit:Auth:LoginWindowMinutes"] = "1",
                ["RateLimit:Auth:RefreshPermitLimit"] = "10000",
                ["RateLimit:Auth:RefreshWindowMinutes"] = "1",
                ["RateLimit:Auth:RegisterPermitLimit"] = "10000",
                ["RateLimit:Auth:RegisterWindowMinutes"] = "1",
                ["ADMIN__EMAIL"] = "admin@test.com",
                ["ADMIN__PASSWORD"] = "Admin@123456"
            });
        });

        builder.ConfigureServices((_, services) =>
        {
            // ── DbContext ────────────────────────────────────────────────────
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString())
                       .UseSnakeCaseNamingConvention());

            // ── JWT ──────────────────────────────────────────────────────────
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("test-secret-budgetwise-at-least-32-chars!"));
            });

            // ── Health checks ────────────────────────────────────────────────
            // AddNpgSql registra via IConfigureOptions<HealthCheckServiceOptions>.
            // É necessário remover todos esses descritores antes de re-adicionar,
            // caso contrário o mesmo nome "postgres" é registrado duas vezes.
            var hcConfigDescriptors = services
                .Where(d => d.ServiceType == typeof(IConfigureOptions<HealthCheckServiceOptions>))
                .ToList();

            foreach (var d in hcConfigDescriptors)
                services.Remove(d);

            services
                .AddHealthChecks()
                .AddNpgSql(
                    _postgres.GetConnectionString(),
                    name: "postgres",
                    failureStatus: HealthStatus.Unhealthy,
                    timeout: TimeSpan.FromSeconds(3),
                    tags: ["database", "ready"]);

            // ── Rate Limiter ─────────────────────────────────────────────────
            // Remove configurações originais e registra políticas com limites ilimitados para testes
            var rateLimiterConfigDescriptors = services
                .Where(d => d.ServiceType == typeof(IConfigureOptions<RateLimiterOptions>)
                         || d.ServiceType == typeof(IPostConfigureOptions<RateLimiterOptions>))
                .ToList();

            foreach (var d in rateLimiterConfigDescriptors)
                services.Remove(d);

            services.Configure<RateLimiterOptions>(options =>
            {
                options.AddPolicy("AuthLogin", _ =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        "test", _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = int.MaxValue,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.AddPolicy("AuthRefresh", _ =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        "test", _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = int.MaxValue,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.AddPolicy("AuthRegister", _ =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        "test", _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = int.MaxValue,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });
        });

        builder.UseEnvironment("Test");
    }

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();
}