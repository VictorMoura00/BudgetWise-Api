using BudgetWise.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BudgetWise.Api.Extensions;

public static class ExceptionHandlerExtensions
{
    public static IServiceCollection AddGlobalErrorHandler(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static WebApplication UseGlobalErrorHandler(this WebApplication app)
    {
        app.UseExceptionHandler();
        return app;
    }
}

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case NotFoundException notFound:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource Not Found",
                    Detail = notFound.Message
                }, cancellationToken);
                return true;

            case ForbiddenException forbidden:
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = forbidden.Message
                }, cancellationToken);
                return true;

            case DomainException domain:
                httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Business Rule Violation",
                    Detail = domain.Message
                }, cancellationToken);
                return true;

            default:
                logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

                var detail = environment.IsProduction()
                    ? "An unexpected error occurred. Please try again later."
                    : $"{exception.Message}\n\n{exception.StackTrace}";

                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = detail
                }, cancellationToken);
                return true;
        }
    }
}