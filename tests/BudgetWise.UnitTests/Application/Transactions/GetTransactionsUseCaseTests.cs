using BudgetWise.Application.Transactions.DTOs;
using BudgetWise.Application.Transactions.UseCases;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Transactions;

public sealed class GetTransactionsUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly GetTransactionsUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public GetTransactionsUseCaseTests()
    {
        _sut = new GetTransactionsUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFilters_ReturnsPaginatedResult()
    {
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today),
            Transaction.Create(UserId, "Aluguel", 1200m, TransactionType.Expense, Today)
        };
        var paged = new PaginatedList<Transaction>(transactions, 2, 1, 20);

        _repository.GetTransactionsForUserAsync(
                UserId, 1, 20, null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(paged);

        var request = new GetTransactionsRequest(1, 20);
        var result = await _sut.ExecuteAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithFilters_PassesFiltersToRepository()
    {
        var categoryId = Guid.NewGuid();
        var paged = new PaginatedList<Transaction>([], 0, 1, 20);

        _repository.GetTransactionsForUserAsync(
                UserId, 1, 20,
                TransactionType.Expense, categoryId,
                Today, Today.AddDays(30), true, null, null,
                Arg.Any<CancellationToken>())
            .Returns(paged);

        var request = new GetTransactionsRequest(1, 20, TransactionType.Expense, categoryId, Today, Today.AddDays(30), true);
        var result = await _sut.ExecuteAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).GetTransactionsForUserAsync(
            UserId, 1, 20,
            TransactionType.Expense, categoryId,
            Today, Today.AddDays(30), true, null, null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPaging_NormalizesToMinimum()
    {
        var paged = new PaginatedList<Transaction>([], 0, 1, 20);
        _repository.GetTransactionsForUserAsync(
                UserId, 1, 20, null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(paged);

        var request = new GetTransactionsRequest(0, -5);
        var result = await _sut.ExecuteAsync(request, UserId);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).GetTransactionsForUserAsync(
            UserId, 1, 20, null, null, null, null, null, null, null, Arg.Any<CancellationToken>());
    }
}
