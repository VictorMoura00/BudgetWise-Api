using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Exceptions;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class SharedExpenseAggregateTests
{
    private static readonly Guid TransactionId = Guid.NewGuid();
    private static readonly Guid FamilyGroupId = Guid.NewGuid();
    private static readonly Guid CreatedBy = Guid.NewGuid();

    [Fact]
    public void Create_WithZeroAmount_ShouldThrowDomainException()
    {
        var act = () => SharedExpense.Create(TransactionId, FamilyGroupId, 0m, CreatedBy);

        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrowDomainException()
    {
        var act = () => SharedExpense.Create(TransactionId, FamilyGroupId, -100m, CreatedBy);

        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void AddParticipant_ShouldAddToCollection()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 300m, CreatedBy);
        var userId = Guid.NewGuid();

        expense.AddParticipant(userId, 100m);

        expense.Participants.Should().ContainSingle(p => p.UserId == userId);
    }

    [Fact]
    public void AddParticipant_ShouldReturnParticipantWithCorrectValues()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 300m, CreatedBy);
        var userId = Guid.NewGuid();

        var participant = expense.AddParticipant(userId, 150m);

        participant.UserId.Should().Be(userId);
        participant.AmountOwed.Should().Be(150m);
        participant.IsSettled.Should().BeFalse();
    }

    [Fact]
    public void AddParticipant_WithZeroAmount_ShouldThrowDomainException()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 300m, CreatedBy);

        var act = () => expense.AddParticipant(Guid.NewGuid(), 0m);

        act.Should().Throw<DomainException>()
            .WithMessage("*Amount owed must be greater than zero*");
    }

    [Fact]
    public void AddParticipant_WhenAlreadyParticipant_ShouldThrowDomainException()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 300m, CreatedBy);
        var userId = Guid.NewGuid();
        expense.AddParticipant(userId, 100m);

        var act = () => expense.AddParticipant(userId, 50m);

        act.Should().Throw<DomainException>()
            .WithMessage("*already a participant*");
    }

    [Fact]
    public void AddParticipant_WhenExceedsTotalAmount_ShouldThrowDomainException()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 100m, CreatedBy);
        expense.AddParticipant(Guid.NewGuid(), 80m);

        var act = () => expense.AddParticipant(Guid.NewGuid(), 30m);

        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot assign*");
    }

    [Fact]
    public void AddParticipant_ExactlyTotalAmount_ShouldSucceed()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 300m, CreatedBy);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        expense.AddParticipant(user1, 100m);
        expense.AddParticipant(user2, 100m);
        expense.AddParticipant(user3, 100m);

        expense.Participants.Should().HaveCount(3);
    }

    [Fact]
    public void AddParticipant_ShouldUpdateUpdatedAt()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 300m, CreatedBy);
        var before = expense.UpdatedAt;

        expense.AddParticipant(Guid.NewGuid(), 100m);

        expense.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
