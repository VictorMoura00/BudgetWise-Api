using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Exceptions;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class TransactionValidationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDescription_ShouldThrowDomainException(string description)
    {
        var act = () => Transaction.Create(UserId, description, 100m, TransactionType.Expense, Today);

        act.Should().Throw<DomainException>()
            .WithMessage("*description cannot be empty*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999.99)]
    public void Create_WithNonPositiveAmount_ShouldThrowDomainException(decimal amount)
    {
        var act = () => Transaction.Create(UserId, "Compra", amount, TransactionType.Expense, Today);

        act.Should().Throw<DomainException>()
            .WithMessage("*amount must be greater than zero*");
    }

    [Fact]
    public void Update_WithEmptyDescription_ShouldThrowDomainException()
    {
        var transaction = Transaction.Create(UserId, "Original", 100m, TransactionType.Expense, Today);

        var act = () => transaction.Update(
            "", 100m, TransactionType.Expense, Today, null, null, RecurrenceType.None, null, null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*description cannot be empty*");
    }

    [Fact]
    public void Update_WithZeroAmount_ShouldThrowDomainException()
    {
        var transaction = Transaction.Create(UserId, "Original", 100m, TransactionType.Expense, Today);

        var act = () => transaction.Update(
            "Valid", 0m, TransactionType.Expense, Today, null, null, RecurrenceType.None, null, null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*amount must be greater than zero*");
    }

    [Fact]
    public void Create_WithPositiveAmount_ShouldSucceed()
    {
        var transaction = Transaction.Create(UserId, "Salário", 0.01m, TransactionType.Income, Today);

        transaction.Amount.Should().Be(0.01m);
    }
}
