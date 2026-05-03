using BudgetWise.Application.Interfaces;
using BudgetWise.Application.SharedExpenses.UseCases;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.SharedExpenses;

public sealed class GetSharedExpenseSummaryUseCaseTests
{
    private readonly IFamilyGroupRepository _familyGroupRepository = Substitute.For<IFamilyGroupRepository>();
    private readonly ISharedExpenseRepository _sharedExpenseRepository = Substitute.For<ISharedExpenseRepository>();
    private readonly IUserLookupService _userLookupService = Substitute.For<IUserLookupService>();
    private readonly GetSharedExpenseSummaryUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid ParticipantA = Guid.NewGuid();
    private static readonly Guid ParticipantB = Guid.NewGuid();

    public GetSharedExpenseSummaryUseCaseTests()
    {
        _sut = new GetSharedExpenseSummaryUseCase(
            _familyGroupRepository,
            _sharedExpenseRepository,
            _userLookupService);
    }

    private void SetupGroupWithOwner()
    {
        var group = FamilyGroup.Create(UserId, "Família Test");
        _familyGroupRepository.GetByIdWithMembersAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(group);
    }

    private static SharedExpense CreateExpense(decimal total, params (Guid userId, decimal amount, bool settled)[] participants)
    {
        var expense = SharedExpense.Create(Guid.NewGuid(), GroupId, total, UserId, "Despesa");
        foreach (var (uid, amount, settled) in participants)
        {
            var p = expense.AddParticipant(uid, amount);
            if (settled) p.Settle();
        }
        return expense;
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotMember_ReturnsForbiddenError()
    {
        var outsider = Guid.NewGuid();
        SetupGroupWithOwner();

        var result = await _sut.ExecuteAsync(GroupId, outsider);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FamilyGroup.NotMember");
    }

    [Fact]
    public async Task ExecuteAsync_WhenGroupNotFound_ReturnsForbiddenError()
    {
        _familyGroupRepository.GetByIdWithMembersAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns((FamilyGroup?)null);

        var result = await _sut.ExecuteAsync(GroupId, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FamilyGroup.NotMember");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoExpenses_ReturnsZeroedSummary()
    {
        SetupGroupWithOwner();
        _sharedExpenseRepository.GetAllForGroupAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns([]);
        _userLookupService.GetDisplayNamesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>());

        var result = await _sut.ExecuteAsync(GroupId, UserId);

        result.IsSuccess.Should().BeTrue();
        var summary = result.Value;
        summary.TotalExpenses.Should().Be(0);
        summary.TotalAmount.Should().Be(0m);
        summary.TotalSettled.Should().Be(0m);
        summary.TotalPending.Should().Be(0m);
        summary.FullySettledCount.Should().Be(0);
        summary.PartiallySettledCount.Should().Be(0);
        summary.UnsettledCount.Should().Be(0);
        summary.ParticipantTotals.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ComputesTotalsCorrectly()
    {
        SetupGroupWithOwner();

        var expenses = new List<SharedExpense>
        {
            // fully settled
            CreateExpense(200m, (ParticipantA, 100m, true), (ParticipantB, 100m, true)),
            // partially settled
            CreateExpense(300m, (ParticipantA, 150m, true), (ParticipantB, 150m, false)),
            // unsettled
            CreateExpense(100m, (ParticipantA, 50m, false), (ParticipantB, 50m, false)),
        };

        _sharedExpenseRepository.GetAllForGroupAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(expenses);

        _userLookupService.GetDisplayNamesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>
            {
                [ParticipantA] = "Alice",
                [ParticipantB] = "Bob",
            });

        var result = await _sut.ExecuteAsync(GroupId, UserId);

        result.IsSuccess.Should().BeTrue();
        var summary = result.Value;

        summary.TotalExpenses.Should().Be(3);
        summary.TotalAmount.Should().Be(600m);
        summary.TotalSettled.Should().Be(350m);   // 100+100+150
        summary.TotalPending.Should().Be(250m);   // 150+50+50
        summary.FullySettledCount.Should().Be(1);
        summary.PartiallySettledCount.Should().Be(1);
        summary.UnsettledCount.Should().Be(1);
        summary.ParticipantTotals.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_ParticipantTotals_AggregatesAcrossExpenses()
    {
        SetupGroupWithOwner();

        var expenses = new List<SharedExpense>
        {
            CreateExpense(200m, (ParticipantA, 100m, true), (ParticipantB, 100m, false)),
            CreateExpense(200m, (ParticipantA, 80m, false)),
        };

        _sharedExpenseRepository.GetAllForGroupAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(expenses);

        _userLookupService.GetDisplayNamesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>
            {
                [ParticipantA] = "Alice",
                [ParticipantB] = "Bob",
            });

        var result = await _sut.ExecuteAsync(GroupId, UserId);

        var alice = result.Value.ParticipantTotals.Single(p => p.UserId == ParticipantA);
        alice.AmountOwed.Should().Be(180m);
        alice.AmountSettled.Should().Be(100m);
        alice.AmountPending.Should().Be(80m);

        var bob = result.Value.ParticipantTotals.Single(p => p.UserId == ParticipantB);
        bob.AmountOwed.Should().Be(100m);
        bob.AmountSettled.Should().Be(0m);
        bob.AmountPending.Should().Be(100m);
    }

    [Fact]
    public async Task ExecuteAsync_ParticipantTotals_UseFallbackWhenUserNameMissing()
    {
        SetupGroupWithOwner();

        var unknownUser = Guid.NewGuid();
        var expenses = new List<SharedExpense>
        {
            CreateExpense(100m, (unknownUser, 100m, false)),
        };

        _sharedExpenseRepository.GetAllForGroupAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(expenses);

        _userLookupService.GetDisplayNamesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>());

        var result = await _sut.ExecuteAsync(GroupId, UserId);

        var participant = result.Value.ParticipantTotals.Single();
        participant.UserName.Should().Be(unknownUser.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_BatchFetchesUserNames_OnlyOnce()
    {
        SetupGroupWithOwner();

        _sharedExpenseRepository.GetAllForGroupAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns([CreateExpense(100m, (ParticipantA, 50m, false), (ParticipantB, 50m, false))]);

        _userLookupService.GetDisplayNamesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>());

        await _sut.ExecuteAsync(GroupId, UserId);

        await _userLookupService.Received(1)
            .GetDisplayNamesByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }
}
