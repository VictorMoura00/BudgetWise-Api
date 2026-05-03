using BudgetWise.Application.SharedExpenses.DTOs;
using BudgetWise.Application.SharedExpenses.UseCases;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.SharedExpenses;

public sealed class CreateSharedExpenseUseCaseTests
{
    private readonly IFamilyGroupRepository _familyGroupRepository = Substitute.For<IFamilyGroupRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ISharedExpenseRepository _sharedExpenseRepository = Substitute.For<ISharedExpenseRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateSharedExpenseUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public CreateSharedExpenseUseCaseTests()
    {
        _sut = new CreateSharedExpenseUseCase(
            _familyGroupRepository,
            _transactionRepository,
            _sharedExpenseRepository,
            _categoryRepository,
            _unitOfWork);
    }

    private FamilyGroup CreateGroupWithOwner()
    {
        var group = FamilyGroup.Create(UserId, "Família Silva");
        _familyGroupRepository.GetByIdWithMembersAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(group);
        return group;
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ReturnsSharedExpenseResponse()
    {
        CreateGroupWithOwner();
        var request = new CreateSharedExpenseRequest(
            "Conta de luz",
            200m,
            Today,
            null,
            [new ParticipantRequest(MemberId, 100m)]);

        var result = await _sut.ExecuteAsync(GroupId, request, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Conta de luz");
        result.Value.TotalAmount.Should().Be(200m);
        result.Value.TotalPending.Should().Be(100m);
        result.Value.Participants.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotMember_ReturnsForbiddenError()
    {
        var outsider = Guid.NewGuid();
        CreateGroupWithOwner();
        var request = new CreateSharedExpenseRequest(
            "Despesa",
            100m,
            Today,
            null,
            [new ParticipantRequest(MemberId, 100m)]);

        var result = await _sut.ExecuteAsync(GroupId, request, outsider);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FamilyGroup.NotMember");
    }

    [Fact]
    public async Task ExecuteAsync_WhenGroupNotFound_ReturnsForbiddenError()
    {
        _familyGroupRepository.GetByIdWithMembersAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns((FamilyGroup?)null);
        var request = new CreateSharedExpenseRequest(
            "Despesa",
            100m,
            Today,
            null,
            [new ParticipantRequest(MemberId, 100m)]);

        var result = await _sut.ExecuteAsync(GroupId, request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FamilyGroup.NotMember");
    }

    [Fact]
    public async Task ExecuteAsync_WithIncomeCategoryId_ReturnsValidationError()
    {
        CreateGroupWithOwner();
        var categoryId = Guid.NewGuid();
        var category = Category.CreatePersonal(UserId, "Salário", categoryType: CategoryType.Income);
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns(category);

        var request = new CreateSharedExpenseRequest(
            "Despesa",
            100m,
            Today,
            categoryId,
            [new ParticipantRequest(MemberId, 100m)]);

        var result = await _sut.ExecuteAsync(GroupId, request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SharedExpense.IncompatibleCategory");
    }

    [Fact]
    public async Task ExecuteAsync_WithCategoryNotFound_ReturnsNotFoundError()
    {
        CreateGroupWithOwner();
        var categoryId = Guid.NewGuid();
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var request = new CreateSharedExpenseRequest(
            "Despesa",
            100m,
            Today,
            categoryId,
            [new ParticipantRequest(MemberId, 100m)]);

        var result = await _sut.ExecuteAsync(GroupId, request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SharedExpense.CategoryNotFound");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistAndCommit()
    {
        CreateGroupWithOwner();
        var request = new CreateSharedExpenseRequest(
            "Conta de água",
            150m,
            Today,
            null,
            [new ParticipantRequest(MemberId, 75m)]);

        await _sut.ExecuteAsync(GroupId, request, UserId);

        await _transactionRepository.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _sharedExpenseRepository.Received(1).AddAsync(Arg.Any<SharedExpense>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
