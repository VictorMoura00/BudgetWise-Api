using System.Security.Claims;
using Serilog;
using Serilog.Formatting.Json;

namespace BudgetWise.Api.Extensions;

public static class SerilogExtensions
{
    public static void AddSerilogConfiguration(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName();

        if (builder.Environment.IsDevelopment())
        {
            // Dev: logs legíveis no console
            loggerConfig.WriteTo.Console();
        }
        else
        {
            // Produção: logs estruturados em JSON no console (para coleta por agentes externos)
            loggerConfig.WriteTo.Console(new JsonFormatter());
        }

        // Seq: sempre ativo quando a URL estiver configurada
        var seqUrl = builder.Configuration["Seq:ServerUrl"];

        if (!string.IsNullOrEmpty(seqUrl))
        {
            loggerConfig.WriteTo.Seq(seqUrl);
        }

        var logger = loggerConfig.CreateLogger();

        builder.Logging.AddSerilog(logger);
        builder.Host.UseSerilog(logger);
    }

    public static WebApplication UseSerilogRequestLoggingWithContext(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                    diagnosticContext.Set("UserId", userId);

                diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestPath", httpContext.Request.Path);
                diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
            };
        });

        return app;
    }
}