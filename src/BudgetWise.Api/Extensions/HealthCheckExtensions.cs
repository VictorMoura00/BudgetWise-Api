using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BudgetWise.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        var builder = services.AddHealthChecks();

        if (!string.IsNullOrEmpty(connectionString))
        {
            builder.AddNpgSql(
                connectionString,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                timeout: TimeSpan.FromSeconds(3),
                tags: ["database", "ready"]);
        }

        return services;
    }

    public static WebApplication UseHealthMonitoring(this WebApplication app)
    {
        // Liveness: a API está no ar?
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false // Não verifica dependências, só se a API responde
        });

        // Readiness: a API está pronta para receber tráfego? (banco acessível)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        // Geral: resumo de todos os checks
        app.MapHealthChecks("/health");

        return app;
    }
}