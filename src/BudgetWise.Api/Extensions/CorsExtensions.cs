namespace BudgetWise.Api.Extensions;

public static class CorsExtensions
{
    private const string PolicyName = "DefaultCors";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origensPermitidas = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (origensPermitidas.Length == 0)
        {
            return services;
        }

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, builder =>
            {
                builder
                    .WithOrigins(origensPermitidas)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    public static WebApplication UseCorsPolicy(this WebApplication app)
    {
        var origensPermitidas = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (origensPermitidas.Length > 0)
        {
            app.UseCors(PolicyName);
        }

        return app;
    }
}
