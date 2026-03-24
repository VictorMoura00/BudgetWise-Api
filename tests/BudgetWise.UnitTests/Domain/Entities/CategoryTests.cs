using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Exceptions;
using BudgetWise.Domain.ValueObjects;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class CategoryTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void CreateSystem_ShouldSetIsSystemTrue_AndNullUserId()
    {
        var category = Category.CreateSystem("Alimentação", "🍔", "Gastos com comida");

        category.Name.Should().Be("Alimentação");
        category.Icon.Should().Be("🍔");
        category.Description.Should().Be("Gastos com comida");
        category.IsSystem.Should().BeTrue();
        category.UserId.Should().BeNull();
        category.IsActive.Should().BeTrue();
        category.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateSystem_WithOnlyName_ShouldSetNullOptionals()
    {
        var category = Category.CreateSystem("Transporte");

        category.Icon.Should().BeNull();
        category.Description.Should().BeNull();
        category.Color.Should().BeNull();
    }

    [Fact]
    public void CreatePersonal_ShouldSetUserIdAndIsSystemFalse()
    {
        var color = HexColor.Create("#FF5733").Value;
        var category = Category.CreatePersonal(UserId, "Lazer", "Entretenimento", "🎮", color);

        category.Name.Should().Be("Lazer");
        category.Description.Should().Be("Entretenimento");
        category.Icon.Should().Be("🎮");
        category.Color.Should().Be(color);
        category.IsSystem.Should().BeFalse();
        category.UserId.Should().Be(UserId);
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldChangeAllFields()
    {
        var category = Category.CreatePersonal(UserId, "Old");
        var newColor = HexColor.Create("#AABBCC").Value;
        var before = category.UpdatedAt;

        category.Update("New", "Desc", "🆕", newColor);

        category.Name.Should().Be("New");
        category.Description.Should().Be("Desc");
        category.Icon.Should().Be("🆕");
        category.Color.Should().Be(newColor);
        category.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Update_WithNullOptionals_ShouldClearFields()
    {
        var category = Category.CreatePersonal(UserId, "Old", "Desc", "🎮");

        category.Update("New", null, null, null);

        category.Description.Should().BeNull();
        category.Icon.Should().BeNull();
        category.Color.Should().BeNull();
    }

    [Fact]
    public void Deactivate_WhenPersonal_ShouldSetIsActiveFalse()
    {
        var category = Category.CreatePersonal(UserId, "Lazer");

        category.Deactivate();

        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenSystem_ShouldThrowDomainException()
    {
        var category = Category.CreateSystem("Alimentação");

        var act = () => category.Deactivate();

        act.Should().Throw<DomainException>()
            .WithMessage("*System categories*");
    }

    [Fact]
    public void CreatePersonal_ShouldInitializeEmptyTransactionsCollection()
    {
        var category = Category.CreatePersonal(UserId, "Test");

        category.Transactions.Should().BeEmpty();
    }
}
