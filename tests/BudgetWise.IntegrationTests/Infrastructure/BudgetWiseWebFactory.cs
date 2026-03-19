using BudgetWise.Infrastructure.Persistence;
using HealthChecks.NpgSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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
                ["Seq:ServerUrl"] = ""
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
        });

        builder.UseEnvironment("Test");
    }

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();
}