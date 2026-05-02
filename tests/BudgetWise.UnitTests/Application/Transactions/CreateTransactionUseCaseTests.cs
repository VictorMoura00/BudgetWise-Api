using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Application.Transactions.UseCases;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Transactions;

public sealed class CreateTransactionUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly CreateTransactionUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public CreateTransactionUseCaseTests()
    {
        _sut = new CreateTransactionUseCase(_repository, _unitOfWork, _categoryRepository);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ReturnsTransactionResponse()
    {
        var request = new CreateTransactionRequest(
            "Salário", 5000m, TransactionType.Income, Today,
            null, null, RecurrenceType.None, null, false, null, null);

        var result = await _sut.ExecuteAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Salário");
        result.Value.Amount.Should().Be(5000m);
        result.Value.Type.Should().Be(TransactionType.Income);
        result.Value.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistAndCommit()
    {
        var request = new CreateTransactionRequest(
            "Aluguel", 1200m, TransactionType.Expense, Today,
            null, null, RecurrenceType.None, null, false, PaymentMethod.Pix, null);

        await _sut.ExecuteAsync(request, UserId);

        await _repository.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithAllOptionalFields_MapsCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var familyGroupId = Guid.NewGuid();
        var endDate = Today.AddMonths(6);

        var category = Category.CreatePersonal(UserId, "Moradia", categoryType: CategoryType.Expense);
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns(category);

        var request = new CreateTransactionRequest(
            "Aluguel", 1200m, TransactionType.Expense, Today,
            categoryId, "Parcela 1/12", RecurrenceType.Monthly,
            endDate, true, PaymentMethod.Ted, familyGroupId);

        var result = await _sut.ExecuteAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.Notes.Should().Be("Parcela 1/12");
        result.Value.RecurrenceType.Should().Be(RecurrenceType.Monthly);
        result.Value.RecurrenceEndDate.Should().Be(endDate);
        result.Value.IsConfirmed.Should().BeTrue();
        result.Value.PaymentMethod.Should().Be(PaymentMethod.Ted);
        result.Value.FamilyGroupId.Should().Be(familyGroupId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryNotFound_ReturnsCategoryNotFoundError()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var request = new CreateTransactionRequest(
            "Teste", 100m, TransactionType.Expense, Today,
            categoryId, null, RecurrenceType.None, null, false, null, null);

        var result = await _sut.ExecuteAsync(request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.CategoryNotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryIncompatible_ReturnsIncompatibleCategoryError()
    {
        var categoryId = Guid.NewGuid();
        var incomeCategory = Category.CreatePersonal(UserId, "Salário", categoryType: CategoryType.Income);
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns(incomeCategory);

        var request = new CreateTransactionRequest(
            "Pagamento", 500m, TransactionType.Expense, Today,
            categoryId, null, RecurrenceType.None, null, false, null, null);

        var result = await _sut.ExecuteAsync(request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.IncompatibleCategory");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryIsBoth_AcceptsAnyTransactionType()
    {
        var categoryId = Guid.NewGuid();
        var bothCategory = Category.CreatePersonal(UserId, "Transferência", categoryType: CategoryType.Both);
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns(bothCategory);

        var expenseRequest = new CreateTransactionRequest(
            "Transferência saída", 300m, TransactionType.Expense, Today,
            categoryId, null, RecurrenceType.None, null, false, null, null);
        var incomeRequest = new CreateTransactionRequest(
            "Transferência entrada", 300m, TransactionType.Income, Today,
            categoryId, null, RecurrenceType.None, null, false, null, null);

        var expenseResult = await _sut.ExecuteAsync(expenseRequest, UserId);
        var incomeResult = await _sut.ExecuteAsync(incomeRequest, UserId);

        expenseResult.IsSuccess.Should().BeTrue();
        incomeResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenExpenseCategoryUsedWithExpense_Succeeds()
    {
        var categoryId = Guid.NewGuid();
        var expenseCategory = Category.CreatePersonal(UserId, "Alimentação", categoryType: CategoryType.Expense);
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns(expenseCategory);

        var request = new CreateTransactionRequest(
            "Supermercado", 200m, TransactionType.Expense, Today,
            categoryId, null, RecurrenceType.None, null, false, null, null);

        var result = await _sut.ExecuteAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncomeCategoryUsedWithIncome_Succeeds()
    {
        var categoryId = Guid.NewGuid();
        var incomeCategory = Category.CreatePersonal(UserId, "Salário", categoryType: CategoryType.Income);
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns(incomeCategory);

        var request = new CreateTransactionRequest(
            "Pagamento mensal", 5000m, TransactionType.Income, Today,
            categoryId, null, RecurrenceType.None, null, false, null, null);

        var result = await _sut.ExecuteAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
    }
}
