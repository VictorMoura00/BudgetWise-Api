using BudgetWise.Application.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BudgetWise.Infrastructure.Seeds;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["ADMIN__EMAIL"];
        var password = configuration["ADMIN__PASSWORD"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminSeeder: ADMIN__EMAIL ou ADMIN__PASSWORD não configurados, seed ignorado.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (existing.Role != UserRole.Admin)
            {
                existing.Role = UserRole.Admin;
                await userManager.UpdateAsync(existing);
                logger.LogInformation("AdminSeeder: usuário {Email} promovido para Admin.", email);
            }
            else
            {
                logger.LogInformation("AdminSeeder: admin já existe, seed ignorado.");
            }

            return;
        }

        var admin = new ApplicationUser
        {
            FullName = "Administrador",
            Email = email,
            UserName = email,
            Role = UserRole.Admin,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
            logger.LogInformation("AdminSeeder: admin {Email} criado com sucesso.", email);
        else
            logger.LogError("AdminSeeder: falha ao criar admin — {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}
