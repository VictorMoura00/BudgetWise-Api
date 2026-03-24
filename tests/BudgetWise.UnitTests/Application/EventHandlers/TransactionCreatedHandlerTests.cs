using BudgetWise.Application.Transactions.EventHandlers;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.EventHandlers;

public sealed class TransactionCreatedHandlerTests
{
    private readonly ILogger<TransactionCreatedHandler> _logger =
        Substitute.For<ILogger<TransactionCreatedHandler>>();

    private readonly TransactionCreatedHandler _sut;

    public TransactionCreatedHandlerTests()
    {
        _sut = new TransactionCreatedHandler(_logger);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteWithoutThrowing()
    {
        var ev = new TransactionCreatedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            500m,
            TransactionType.Income,
            DateOnly.FromDateTime(DateTime.UtcNow));

        var act = async () => await _sut.HandleAsync(ev, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldLogInformation()
    {
        var transactionId = Guid.NewGuid();
        var ev = new TransactionCreatedEvent(
            transactionId,
            Guid.NewGuid(),
            250m,
            TransactionType.Expense,
            DateOnly.FromDateTime(DateTime.UtcNow));

        await _sut.HandleAsync(ev, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(transactionId.ToString())),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCompletedTask()
    {
        var ev = new TransactionCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), 100m,
            TransactionType.Income,
            DateOnly.FromDateTime(DateTime.UtcNow));

        var result = _sut.HandleAsync(ev, CancellationToken.None);

        result.IsCompleted.Should().BeTrue();
        await result;
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_ShouldStillComplete()
    {
        var ev = new TransactionCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), 100m,
            TransactionType.Income,
            DateOnly.FromDateTime(DateTime.UtcNow));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Handler currently does no async I/O so cancellation is not observed — just completes
        var act = async () => await _sut.HandleAsync(ev, cts.Token);

        await act.Should().NotThrowAsync();
    }
}
