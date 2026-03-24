using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Common;

public sealed class EntityTests
{
    private static Transaction CreateTransaction() =>
        Transaction.Create(
            Guid.NewGuid(),
            "Test",
            100m,
            TransactionType.Expense,
            DateOnly.FromDateTime(DateTime.UtcNow));

    [Fact]
    public void NewEntity_ShouldHaveNonEmptyId()
    {
        var entity = CreateTransaction();

        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void NewEntity_ShouldHaveCreatedAtSetToNow()
    {
        var entity = CreateTransaction();

        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void NewEntity_ShouldHaveUpdatedAtSetToNow()
    {
        var entity = CreateTransaction();

        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Equals_SameInstance_ShouldReturnTrue()
    {
        var entity = CreateTransaction();

        entity.Equals(entity).Should().BeTrue();
    }

    [Fact]
    public void Equals_TwoDifferentEntities_ShouldReturnFalse()
    {
        var a = CreateTransaction();
        var b = CreateTransaction();

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var entity = CreateTransaction();

        entity.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameEntity_ShouldBeConsistent()
    {
        var entity = CreateTransaction();

        entity.GetHashCode().Should().Be(entity.GetHashCode());
    }

    [Fact]
    public void GetHashCode_TwoDifferentEntities_ShouldBeDifferent()
    {
        var a = CreateTransaction();
        var b = CreateTransaction();

        // Hash codes podem colidir, mas para GUIDs distintos é extremamente improvável
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }
}
