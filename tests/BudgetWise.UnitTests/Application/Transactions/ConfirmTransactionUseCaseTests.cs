using BudgetWise.Application.Transactions.UseCases;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.Transactions;

public sealed class ConfirmTransactionUseCaseTests
{
    private readonly ITransactionRepository _repository = Substitute.For<ITransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ConfirmTransactionUseCase _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 5, 2);

    public ConfirmTransactionUseCaseTests()
    {
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero));
        _sut = new ConfirmTransactionUseCase(_repository, _unitOfWork, _timeProvider);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPending_ConfirmsAndReturnsResponse()
    {
        var transaction = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today, isConfirmed: false);
        _repository.GetByIdForUserAsync(transaction.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var result = await _sut.ExecuteAsync(transaction.Id, null, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsConfirmed.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaidAtNotProvided_UsesTodayFromTimeProvider()
    {
        var transaction = Transaction.Create(UserId, "Aluguel", 1200m, TransactionType.Expense, Today, isConfirmed: false);
        _repository.GetByIdForUserAsync(transaction.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var result = await _sut.ExecuteAsync(transaction.Id, null, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.PaidAt.Should().Be(Today);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaidAtProvided_UsesProvidedDate()
    {
        var transaction = Transaction.Create(UserId, "Fatura", 800m, TransactionType.Expense, Today, isConfirmed: false);
        _repository.GetByIdForUserAsync(transaction.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var dataPagamento = Today.AddDays(-3);
        var result = await _sut.ExecuteAsync(transaction.Id, dataPagamento, UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.PaidAt.Should().Be(dataPagamento);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyConfirmed_ReturnsAlreadyConfirmedError()
    {
        var transaction = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today, isConfirmed: true);
        _repository.GetByIdForUserAsync(transaction.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var result = await _sut.ExecuteAsync(transaction.Id, null, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.AlreadyConfirmed");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdForUserAsync(id, UserId, Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var result = await _sut.ExecuteAsync(id, null, UserId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.NotFound");
    }
}
