using BudgetWise.Application.Transactions.EventHandlers;
using BudgetWise.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BudgetWise.UnitTests.Application.EventHandlers;

public sealed class TransactionConfirmedHandlerTests
{
    private readonly ILogger<TransactionConfirmedHandler> _logger =
        Substitute.For<ILogger<TransactionConfirmedHandler>>();

    private readonly TransactionConfirmedHandler _sut;

    public TransactionConfirmedHandlerTests()
    {
        _sut = new TransactionConfirmedHandler(_logger);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteWithoutThrowing()
    {
        var ev = new TransactionConfirmedEvent(Guid.NewGuid(), Guid.NewGuid());

        var act = async () => await _sut.HandleAsync(ev, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldLogInformation()
    {
        var transactionId = Guid.NewGuid();
        var ev = new TransactionConfirmedEvent(transactionId, Guid.NewGuid());

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
        var ev = new TransactionConfirmedEvent(Guid.NewGuid(), Guid.NewGuid());

        var result = _sut.HandleAsync(ev, CancellationToken.None);

        result.IsCompleted.Should().BeTrue();
        await result;
    }
}
