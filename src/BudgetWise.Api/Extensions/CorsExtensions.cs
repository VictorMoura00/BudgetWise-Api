using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BudgetWise.Api.Extensions;

public static class CorsExtensions
{
    private const string DevPolicyName = "AllowAll";
    private const string ProdPolicyName = "AllowFrontend";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(DevPolicyName, builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });

            options.AddPolicy(ProdPolicyName, builder =>
            {
                builder
                    .WithOrigins("https://budgetwise.app")
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }

    public static WebApplication UseCorsPolicy(this WebApplication app)
    {
        var policyName = app.Environment.IsDevelopment()
            ? DevPolicyName
            : ProdPolicyName;

        app.UseCors(policyName);

        return app;
    }
}