using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace BudgetWise.Api.Extensions;

public static class RateLimitExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var loginLimit = int.TryParse(configuration["RateLimit:Auth:LoginPermitLimit"], out var l) ? l : 5;
        var loginWindow = TimeSpan.FromMinutes(int.TryParse(configuration["RateLimit:Auth:LoginWindowMinutes"], out var lm) ? lm : 1);

        var refreshLimit = int.TryParse(configuration["RateLimit:Auth:RefreshPermitLimit"], out var r) ? r : 10;
        var refreshWindow = TimeSpan.FromMinutes(int.TryParse(configuration["RateLimit:Auth:RefreshWindowMinutes"], out var rm) ? rm : 1);

        var registerLimit = int.TryParse(configuration["RateLimit:Auth:RegisterPermitLimit"], out var reg) ? reg : 5;
        var registerWindow = TimeSpan.FromMinutes(int.TryParse(configuration["RateLimit:Auth:RegisterWindowMinutes"], out var regm) ? regm : 1);

        services.AddRateLimiter(options =>
        {
            options.AddPolicy("AuthLogin", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginLimit,
                        Window = loginWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy("AuthRefresh", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = refreshLimit,
                        Window = refreshWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.AddPolicy("AuthRegister", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = registerLimit,
                        Window = registerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    private static string GetClientKey(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString()
            ?? httpContext.Request.Headers.Host.ToString()
            ?? "anonymous";
    }
}
