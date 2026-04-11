using BudgetWise.Application.Transactions.UseCases;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Transactions;

public sealed class DeleteTransactionUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteTransactionUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public DeleteTransactionUseCaseTests()
    {
        _sut = new DeleteTransactionUseCase(_repository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var transaction = Transaction.Create(UserId, "Compra", 100m, TransactionType.Expense, Today);
        _repository.GetByIdForUserAsync(transaction.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var result = await _sut.ExecuteAsync(transaction.Id, UserId);

        result.IsSuccess.Should().BeTrue();
        transaction.IsDeleted.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
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
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
