using BudgetWise.Domain.Entities;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class TagTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldStoreLowercaseAndTrimmedName()
    {
        var tag = Tag.Create(UserId, "  Férias  ");

        tag.Name.Should().Be("férias");
        tag.UserId.Should().Be(UserId);
        tag.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldNormalizeMixedCaseName()
    {
        var tag = Tag.Create(UserId, "SUPERMERCADO");

        tag.Name.Should().Be("supermercado");
    }

    [Fact]
    public void Rename_ShouldUpdateNameToLowercaseTrimmed()
    {
        var tag = Tag.Create(UserId, "old");

        tag.Rename("  NEW NAME  ");

        tag.Name.Should().Be("new name");
    }

    [Fact]
    public void Rename_ShouldUpdateUpdatedAt()
    {
        var tag = Tag.Create(UserId, "old");
        var before = tag.UpdatedAt;

        tag.Rename("new");

        tag.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Create_ShouldInitializeEmptyTransactionTagsCollection()
    {
        var tag = Tag.Create(UserId, "test");

        tag.TransactionTags.Should().BeEmpty();
    }
}
