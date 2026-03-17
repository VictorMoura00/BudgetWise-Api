using System.Reflection;
using BudgetWise.Application.Interfaces;
using BudgetWise.Infrastructure.Services;

namespace BudgetWise.Api.Extensions;

public static class UseCaseExtensions
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        var useCaseTypes = typeof(IUseCase).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IUseCase).IsAssignableFrom(t));

        foreach (var type in useCaseTypes)
            services.AddScoped(type);

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}