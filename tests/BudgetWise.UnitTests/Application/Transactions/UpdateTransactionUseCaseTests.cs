using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Application.Transactions.UseCases;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Transactions;

public sealed class UpdateTransactionUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly UpdateTransactionUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public UpdateTransactionUseCaseTests()
    {
        _sut = new UpdateTransactionUseCase(_repository, _unitOfWork, _categoryRepository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFound_UpdatesAndReturnsResponse()
    {
        var transaction = Transaction.Create(UserId, "Original", 100m, TransactionType.Expense, Today);
        _repository.GetByIdForUserAsync(transaction.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var request = new UpdateTransactionRequest(
            "Atualizado", 200m, TransactionType.Income, Today,
            null, null, RecurrenceType.None, null, PaymentMethod.Cash, null);

        var result = await _sut.ExecuteAsync(transaction.Id, request, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Atualizado");
        result.Value.Amount.Should().Be(200m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdForUserAsync(id, UserId, Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var request = new UpdateTransactionRequest(
            "Test", 100m, TransactionType.Expense, Today,
            null, null, RecurrenceType.None, null, null, null);

        var result = await _sut.ExecuteAsync(id, request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.NotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryIncompatible_ReturnsIncompatibleCategoryError()
    {
        var transaction = Transaction.Create(UserId, "Receita", 500m, TransactionType.Income, Today);
        _repository.GetByIdForUserAsync(transaction.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var categoryId = Guid.NewGuid();
        var expenseCategory = Category.CreatePersonal(UserId, "Alimentação", categoryType: CategoryType.Expense);
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns(expenseCategory);

        var request = new UpdateTransactionRequest(
            "Teste", 500m, TransactionType.Income, Today,
            categoryId, null, RecurrenceType.None, null, null, null);

        var result = await _sut.ExecuteAsync(transaction.Id, request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.IncompatibleCategory");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryNotFound_ReturnsCategoryNotFoundError()
    {
        var transaction = Transaction.Create(UserId, "Despesa", 300m, TransactionType.Expense, Today);
        _repository.GetByIdForUserAsync(transaction.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var categoryId = Guid.NewGuid();
        _categoryRepository.GetByIdForUserAsync(categoryId, UserId, Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var request = new UpdateTransactionRequest(
            "Teste", 300m, TransactionType.Expense, Today,
            categoryId, null, RecurrenceType.None, null, null, null);

        var result = await _sut.ExecuteAsync(transaction.Id, request, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.CategoryNotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
