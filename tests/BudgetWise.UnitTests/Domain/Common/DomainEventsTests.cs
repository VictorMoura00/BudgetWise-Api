using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Events;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Common;

public sealed class DomainEventsTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void NewEntity_ShouldHaveEmptyDomainEvents()
    {
        var transaction = Transaction.Create(UserId, "Test", 100m, TransactionType.Income, Today);
        transaction.PopDomainEvents();  // clear the Created event

        transaction.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Transaction_Create_ShouldRaiseTransactionCreatedEvent()
    {
        var transaction = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today);

        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TransactionCreatedEvent>();

        var ev = (TransactionCreatedEvent)transaction.DomainEvents[0];
        ev.TransactionId.Should().Be(transaction.Id);
        ev.UserId.Should().Be(UserId);
        ev.Amount.Should().Be(5000m);
        ev.Type.Should().Be(TransactionType.Income);
        ev.TransactionDate.Should().Be(Today);
    }

    [Fact]
    public void Transaction_Confirm_ShouldRaiseTransactionConfirmedEvent()
    {
        var transaction = Transaction.Create(UserId, "Aluguel", 1200m, TransactionType.Expense, Today);
        transaction.PopDomainEvents();

        transaction.Confirm(Today);

        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TransactionConfirmedEvent>();

        var ev = (TransactionConfirmedEvent)transaction.DomainEvents[0];
        ev.TransactionId.Should().Be(transaction.Id);
        ev.UserId.Should().Be(UserId);
    }

    [Fact]
    public void Transaction_SoftDelete_ShouldRaiseTransactionDeletedEvent()
    {
        var transaction = Transaction.Create(UserId, "Compra", 200m, TransactionType.Expense, Today);
        transaction.PopDomainEvents();

        transaction.SoftDelete();

        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TransactionDeletedEvent>();

        var ev = (TransactionDeletedEvent)transaction.DomainEvents[0];
        ev.TransactionId.Should().Be(transaction.Id);
        ev.UserId.Should().Be(UserId);
        ev.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void PopDomainEvents_ShouldReturnEventsAndClearCollection()
    {
        var transaction = Transaction.Create(UserId, "Test", 50m, TransactionType.Expense, Today);

        var events = transaction.PopDomainEvents();

        events.Should().HaveCount(1);
        transaction.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void PopDomainEvents_CalledTwice_ShouldReturnEmptySecondTime()
    {
        var transaction = Transaction.Create(UserId, "Test", 50m, TransactionType.Expense, Today);

        transaction.PopDomainEvents();
        var second = transaction.PopDomainEvents();

        second.Should().BeEmpty();
    }

    [Fact]
    public void FamilyGroup_AddMember_ShouldRaiseFamilyMemberJoinedEvent()
    {
        var group = FamilyGroup.Create(Guid.NewGuid(), "Família");
        var memberId = Guid.NewGuid();

        group.AddMember(memberId);

        group.DomainEvents.Should().ContainSingle(e => e is FamilyMemberJoinedEvent);

        var ev = group.DomainEvents.OfType<FamilyMemberJoinedEvent>().Single();
        ev.FamilyGroupId.Should().Be(group.Id);
        ev.UserId.Should().Be(memberId);
        ev.Role.Should().Be(FamilyMemberRole.Member);
    }

    [Fact]
    public void FamilyGroup_RemoveMember_ShouldRaiseFamilyMemberRemovedEvent()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var group = FamilyGroup.Create(ownerId, "Família");
        group.AddMember(memberId);
        group.PopDomainEvents();

        group.RemoveMember(memberId, ownerId);

        group.DomainEvents.Should().ContainSingle(e => e is FamilyMemberRemovedEvent);

        var ev = group.DomainEvents.OfType<FamilyMemberRemovedEvent>().Single();
        ev.FamilyGroupId.Should().Be(group.Id);
        ev.RemovedUserId.Should().Be(memberId);
        ev.RemovedByUserId.Should().Be(ownerId);
    }
}
