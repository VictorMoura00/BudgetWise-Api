using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Exceptions;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class TagValidationTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrowDomainException(string name)
    {
        var act = () => Tag.Create(UserId, name);

        act.Should().Throw<DomainException>()
            .WithMessage("*Tag name cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmptyName_ShouldThrowDomainException(string newName)
    {
        var tag = Tag.Create(UserId, "viagem");

        var act = () => tag.Rename(newName);

        act.Should().Throw<DomainException>()
            .WithMessage("*Tag name cannot be empty*");
    }

    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        var tag = Tag.Create(UserId, "Supermercado");

        tag.Name.Should().Be("supermercado");
    }
}
