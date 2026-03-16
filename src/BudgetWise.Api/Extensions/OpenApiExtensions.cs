using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace BudgetWise.Api.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "BudgetWise API";
                document.Info.Version = "v1";
                document.Info.Description = "API de organização financeira pessoal com suporte a grupos familiares.";

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Informe o token JWT no formato: Bearer {token}"
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication UseDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            // Acessível em: http://localhost:8080/scalar/v1
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("BudgetWise API Docs")
                    .WithTheme(ScalarTheme.DeepSpace)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .AddPreferredSecuritySchemes("Bearer");
            });
        }

        return app;
    }
}