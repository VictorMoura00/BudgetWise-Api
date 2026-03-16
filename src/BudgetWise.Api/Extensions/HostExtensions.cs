using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace BudgetWise.Api.Extensions;

public static class HostExtensions
{
    public static WebApplication UseStartupLog(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var server = app.Services.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
            var env = app.Environment;
            var logger = app.Logger;

            logger.LogInformation("-------------------------------------------------");
            logger.LogInformation(" ° {App} is running!", env.ApplicationName);
            logger.LogInformation(" ° Environment: {Environment}", env.EnvironmentName);

            if (addresses is not null)
            {
                foreach (var address in addresses)
                    logger.LogInformation(" ° Listening on: {Address}", address);

                var mainUrl = addresses.FirstOrDefault(x => x.StartsWith("https"))
                              ?? addresses.FirstOrDefault();

                if (env.IsDevelopment() && mainUrl is not null)
                {
                    logger.LogInformation(" ° Documentation: {Url}/scalar/v1", mainUrl);
                    logger.LogInformation(" ° Health Check:  {Url}/health", mainUrl);
                }
            }

            logger.LogInformation("-------------------------------------------------");
        });

        return app;
    }
}