using BudgetWise.Application.SharedExpenses.UseCases;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.SharedExpenses;

public sealed class SettleSharedExpenseParticipantUseCaseTests
{
    private readonly IFamilyGroupRepository _familyGroupRepository = Substitute.For<IFamilyGroupRepository>();
    private readonly ISharedExpenseRepository _sharedExpenseRepository = Substitute.For<ISharedExpenseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SettleSharedExpenseParticipantUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid ParticipantUserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public SettleSharedExpenseParticipantUseCaseTests()
    {
        _sut = new SettleSharedExpenseParticipantUseCase(
            _familyGroupRepository,
            _sharedExpenseRepository,
            _unitOfWork);
    }

    private FamilyGroup SetupGroupWithOwner()
    {
        var group = FamilyGroup.Create(UserId, "Família Test");
        _familyGroupRepository.GetByIdWithMembersAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(group);
        return group;
    }

    private SharedExpense SetupSharedExpense()
    {
        var transactionId = Guid.NewGuid();
        var expense = SharedExpense.Create(transactionId, GroupId, 200m, UserId, "Despesa");
        expense.AddParticipant(ParticipantUserId, 100m);
        return expense;
    }

    [Fact]
    public async Task ExecuteAsync_WithValidParticipant_MarksSettledAndReturnsResponse()
    {
        SetupGroupWithOwner();
        var expense = SetupSharedExpense();
        _sharedExpenseRepository.GetByIdForGroupAsync(expense.Id, GroupId, Arg.Any<CancellationToken>())
            .Returns(expense);

        var result = await _sut.ExecuteAsync(GroupId, expense.Id, ParticipantUserId, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Participants.Should().ContainSingle(p => p.UserId == ParticipantUserId && p.IsSettled);
        result.Value.TotalSettled.Should().Be(100m);
        result.Value.TotalPending.Should().Be(0m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotMember_ReturnsForbiddenError()
    {
        var outsider = Guid.NewGuid();
        SetupGroupWithOwner();

        var result = await _sut.ExecuteAsync(GroupId, Guid.NewGuid(), ParticipantUserId, outsider);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FamilyGroup.NotMember");
    }

    [Fact]
    public async Task ExecuteAsync_WhenExpenseNotFound_ReturnsNotFoundError()
    {
        SetupGroupWithOwner();
        var expenseId = Guid.NewGuid();
        _sharedExpenseRepository.GetByIdForGroupAsync(expenseId, GroupId, Arg.Any<CancellationToken>())
            .Returns((SharedExpense?)null);

        var result = await _sut.ExecuteAsync(GroupId, expenseId, ParticipantUserId, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SharedExpense.NotFound");
    }

    [Fact]
    public async Task ExecuteAsync_WhenParticipantNotInExpense_ReturnsNotFoundError()
    {
        SetupGroupWithOwner();
        var expense = SetupSharedExpense();
        _sharedExpenseRepository.GetByIdForGroupAsync(expense.Id, GroupId, Arg.Any<CancellationToken>())
            .Returns(expense);

        var unknownUserId = Guid.NewGuid();
        var result = await _sut.ExecuteAsync(GroupId, expense.Id, unknownUserId, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SharedExpense.ParticipantNotFound");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadySettled_ReturnsValidationError()
    {
        SetupGroupWithOwner();
        var expense = SetupSharedExpense();
        var participant = expense.Participants.First(p => p.UserId == ParticipantUserId);
        participant.Settle();

        _sharedExpenseRepository.GetByIdForGroupAsync(expense.Id, GroupId, Arg.Any<CancellationToken>())
            .Returns(expense);

        var result = await _sut.ExecuteAsync(GroupId, expense.Id, ParticipantUserId, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SharedExpense.AlreadySettled");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCommitAfterSettling()
    {
        SetupGroupWithOwner();
        var expense = SetupSharedExpense();
        _sharedExpenseRepository.GetByIdForGroupAsync(expense.Id, GroupId, Arg.Any<CancellationToken>())
            .Returns(expense);

        await _sut.ExecuteAsync(GroupId, expense.Id, ParticipantUserId, UserId);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
