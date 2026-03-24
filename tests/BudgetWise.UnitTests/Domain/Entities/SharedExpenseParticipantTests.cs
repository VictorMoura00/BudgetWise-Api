using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Exceptions;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class SharedExpenseParticipantTests
{
    private static readonly Guid SharedExpenseId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var participant = SharedExpenseParticipant.Create(SharedExpenseId, UserId, 150m);

        participant.SharedExpenseId.Should().Be(SharedExpenseId);
        participant.UserId.Should().Be(UserId);
        participant.AmountOwed.Should().Be(150m);
        participant.IsSettled.Should().BeFalse();
        participant.SettledAt.Should().BeNull();
        participant.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Settle_WhenNotSettled_ShouldMarkAsSettled()
    {
        var participant = SharedExpenseParticipant.Create(SharedExpenseId, UserId, 75m);

        participant.Settle();

        participant.IsSettled.Should().BeTrue();
        participant.SettledAt.Should().NotBeNull();
        participant.SettledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Settle_WhenNotSettled_ShouldUpdateUpdatedAt()
    {
        var participant = SharedExpenseParticipant.Create(SharedExpenseId, UserId, 75m);
        var before = participant.UpdatedAt;

        participant.Settle();

        participant.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Settle_WhenAlreadySettled_ShouldThrowDomainException()
    {
        var participant = SharedExpenseParticipant.Create(SharedExpenseId, UserId, 75m);
        participant.Settle();

        var act = () => participant.Settle();

        act.Should().Throw<DomainException>()
            .WithMessage("*already been paid off*");
    }
}
