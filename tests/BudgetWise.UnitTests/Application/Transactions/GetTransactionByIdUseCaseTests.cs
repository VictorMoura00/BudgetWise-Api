using BudgetWise.Application.Transactions.UseCases;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Transactions;

public sealed class GetTransactionByIdUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly GetTransactionByIdUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public GetTransactionByIdUseCaseTests()
    {
        _sut = new GetTransactionByIdUseCase(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFound_ReturnsTransactionResponse()
    {
        var transaction = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today);
        _repository.GetByIdForUserAsync(transaction.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var result = await _sut.ExecuteAsync(transaction.Id, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(transaction.Id);
        result.Value.Description.Should().Be("Salário");
        result.Value.Amount.Should().Be(5000m);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdForUserAsync(id, UserId, Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var result = await _sut.ExecuteAsync(id, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.NotFound");
    }
}
