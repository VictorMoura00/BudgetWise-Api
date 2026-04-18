using BudgetWise.Domain.Entities;
using BudgetWise.Domain.ValueObjects;
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

        static HexColor Color(string hex) => HexColor.Create(hex).Value!;

        var categories = new List<Category>
        {
            // Despesas
            Category.CreateSystem("Alimentação",    icon: "utensils",          color: Color("#EF4444")),
            Category.CreateSystem("Transporte",     icon: "car",               color: Color("#3B82F6")),
            Category.CreateSystem("Moradia",        icon: "house",             color: Color("#8B5CF6")),
            Category.CreateSystem("Saúde",          icon: "heart-pulse",       color: Color("#EC4899")),
            Category.CreateSystem("Lazer",          icon: "tv-2",              color: Color("#F59E0B")),
            Category.CreateSystem("Educação",       icon: "graduation-cap",    color: Color("#10B981")),

            // Receitas
            Category.CreateSystem("Salário",        icon: "banknote",          color: Color("#22C55E")),
            Category.CreateSystem("Freelance",      icon: "briefcase",         color: Color("#06B6D4")),

            // Neutras
            Category.CreateSystem("Investimento",   icon: "trending-up",       color: Color("#6366F1")),
            Category.CreateSystem("Transferência",  icon: "arrow-left-right",  color: Color("#64748B")),
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        logger.LogInformation("CategorySeeder: {Count} system categories seeded.", categories.Count);
    }
}
