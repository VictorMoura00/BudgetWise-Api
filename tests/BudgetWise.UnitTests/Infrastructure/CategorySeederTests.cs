using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Infrastructure.Persistence;
using BudgetWise.Infrastructure.Persistence.Interceptors;
using BudgetWise.Infrastructure.Seeds;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wolverine;

namespace BudgetWise.UnitTests.Infrastructure;

public sealed class CategorySeederTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var interceptor = new DomainEventDispatcherInterceptor(Substitute.For<IMessageBus>());

        return new AppDbContext(options, interceptor);
    }

    [Fact]
    public async Task SeedAsync_WhenNoCategoriesExist_Inserts10SystemCategories()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var logger = NullLogger.Instance;

        // Act
        await CategorySeeder.SeedAsync(context, logger);

        // Assert
        var count = await context.Categories.CountAsync(c => c.IsSystem);
        count.Should().Be(10);
    }

    [Fact]
    public async Task SeedAsync_AllInsertedCategories_HaveIsSystemTrue()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        // Act
        await CategorySeeder.SeedAsync(context, NullLogger.Instance);

        // Assert
        var categories = await context.Categories.ToListAsync();
        categories.Should().AllSatisfy(c => c.IsSystem.Should().BeTrue());
    }

    [Fact]
    public async Task SeedAsync_AllInsertedCategories_HaveNullUserId()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        // Act
        await CategorySeeder.SeedAsync(context, NullLogger.Instance);

        // Assert
        var categories = await context.Categories.ToListAsync();
        categories.Should().AllSatisfy(c => c.UserId.Should().BeNull());
    }

    [Fact]
    public async Task SeedAsync_WhenCalledTwice_DoesNotInsertDuplicates()
    {
        // Arrange
        await using var context = CreateInMemoryContext();

        // Act
        await CategorySeeder.SeedAsync(context, NullLogger.Instance);
        await CategorySeeder.SeedAsync(context, NullLogger.Instance);

        // Assert — idempotente: ainda 10
        var count = await context.Categories.CountAsync();
        count.Should().Be(10);
    }

    [Fact]
    public async Task SeedAsync_ExpenseCategories_HaveCorrectCategoryType()
    {
        await using var context = CreateInMemoryContext();

        await CategorySeeder.SeedAsync(context, NullLogger.Instance);

        var expenseNames = new[] { "Alimentação", "Transporte", "Moradia", "Saúde", "Lazer", "Educação" };
        var expenseCategories = await context.Categories
            .Where(c => expenseNames.Contains(c.Name))
            .ToListAsync();

        expenseCategories.Should().HaveCount(6);
        expenseCategories.Should().AllSatisfy(c => c.CategoryType.Should().Be(CategoryType.Expense));
    }

    [Fact]
    public async Task SeedAsync_IncomeCategories_HaveCorrectCategoryType()
    {
        await using var context = CreateInMemoryContext();

        await CategorySeeder.SeedAsync(context, NullLogger.Instance);

        var incomeNames = new[] { "Salário", "Freelance" };
        var incomeCategories = await context.Categories
            .Where(c => incomeNames.Contains(c.Name))
            .ToListAsync();

        incomeCategories.Should().HaveCount(2);
        incomeCategories.Should().AllSatisfy(c => c.CategoryType.Should().Be(CategoryType.Income));
    }

    [Fact]
    public async Task SeedAsync_NeutralCategories_HaveBothCategoryType()
    {
        await using var context = CreateInMemoryContext();

        await CategorySeeder.SeedAsync(context, NullLogger.Instance);

        var bothNames = new[] { "Investimento", "Transferência" };
        var bothCategories = await context.Categories
            .Where(c => bothNames.Contains(c.Name))
            .ToListAsync();

        bothCategories.Should().HaveCount(2);
        bothCategories.Should().AllSatisfy(c => c.CategoryType.Should().Be(CategoryType.Both));
    }

    [Fact]
    public async Task SeedAsync_WhenSystemCategoriesAlreadyExist_SkipsInsertion()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        await CategorySeeder.SeedAsync(context, NullLogger.Instance);

        // Adiciona uma categoria pessoal para garantir que o count não muda
        context.Categories.Add(Category.CreatePersonal(Guid.NewGuid(), "Minha Categoria"));
        await context.SaveChangesAsync();

        // Act
        await CategorySeeder.SeedAsync(context, NullLogger.Instance);

        // Assert — seed ignorado, somente a pessoal foi adicionada além das 10
        var count = await context.Categories.CountAsync();
        count.Should().Be(11);
    }
}
