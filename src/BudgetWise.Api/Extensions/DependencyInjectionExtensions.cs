using BudgetWise.Application.Auth.Register;
using BudgetWise.Application.Identity;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Interfaces;
using BudgetWise.Infrastructure.Persistence;
using BudgetWise.Infrastructure.Repositories;
using BudgetWise.Infrastructure.Repositories.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Api.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrEmpty(connectionString))
        {
            if (!environment.IsEnvironment("Test"))
                throw new InvalidOperationException("Connection string 'Default' not found.");
            return services;
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();
        });

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Scan automático: qualquer classe que termina com "Repository"
        var repositoryTypes = typeof(CategoryRepository).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.Name.EndsWith("Repository"));

        foreach (var type in repositoryTypes)
        {
            var interfaceType = type.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{type.Name}");

            if (interfaceType is not null)
                services.AddScoped(interfaceType, type);
        }

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                // Requisitos de senha
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // Bloquear conta após tentativas inválidas
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // Email único
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>();

        return services;
    }

    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterUserValidator>();
        return services;
    }
}