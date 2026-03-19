using BudgetWise.Domain.Entities;
using BudgetWise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BudgetWise.Infrastructure.Seeds;

public static class CategorySeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        var alreadySeeded = await context.Categories.AnyAsync(c => c.IsSystem);
        if (alreadySeeded)
        {
            logger.LogInformation("CategorySeeder: system categories already exist, skipping.");
            return;
        }

        logger.LogInformation("CategorySeeder: seeding system categories...");

        var categories = new List<Category>
        {
            Category.CreateSystem("Alimentação",                    icon: "utensils"),
            Category.CreateSystem("Transporte",                     icon: "car"),
            Category.CreateSystem("Moradia",                        icon: "home"),
            Category.CreateSystem("Saúde",                          icon: "heart-pulse"),
            Category.CreateSystem("Lazer",                          icon: "gamepad-2"),
            Category.CreateSystem("Educação",                       icon: "graduation-cap"),
            Category.CreateSystem("Assinaturas e Serviços",         icon: "repeat"),
            Category.CreateSystem("Vestuário",                      icon: "shirt"),
            Category.CreateSystem("Cuidados Pessoais",              icon: "sparkles"),

            Category.CreateSystem("Salário",                        icon: "banknote"),
            Category.CreateSystem("Freelance",                      icon: "briefcase"),
            Category.CreateSystem("Rendimento de Investimentos",    icon: "trending-up"),
            Category.CreateSystem("Aluguel Recebido",               icon: "building"),
            Category.CreateSystem("Presente",                       icon: "gift"),

            Category.CreateSystem("Transferência",                  icon: "arrow-left-right"),
            Category.CreateSystem("Investimento",                   icon: "piggy-bank"),
            Category.CreateSystem("Reembolso",                      icon: "rotate-ccw"),
        };      

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        logger.LogInformation("CategorySeeder: {Count} system categories seeded.", categories.Count);
    }
}