using BudgetWise.Domain.Entities;
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
