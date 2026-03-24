using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Exceptions;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class CategoryActivationTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Activate_WhenInactive_ShouldSetIsActiveTrue()
    {
        var category = Category.CreatePersonal(UserId, "Lazer");
        category.Deactivate();

        category.Activate();

        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldUpdateUpdatedAt()
    {
        var category = Category.CreatePersonal(UserId, "Lazer");
        category.Deactivate();
        var before = category.UpdatedAt;

        category.Activate();

        category.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Activate_WhenSystem_ShouldThrowDomainException()
    {
        var category = Category.CreateSystem("Alimentação");

        var act = () => category.Activate();

        act.Should().Throw<DomainException>()
            .WithMessage("*System categories*");
    }

    [Fact]
    public void Deactivate_ThenActivate_ShouldRestoreActiveState()
    {
        var category = Category.CreatePersonal(UserId, "Viagem");
        category.Deactivate();
        category.IsActive.Should().BeFalse();

        category.Activate();

        category.IsActive.Should().BeTrue();
    }
}
