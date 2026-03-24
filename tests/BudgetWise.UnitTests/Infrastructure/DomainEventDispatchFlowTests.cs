using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Events;
using BudgetWise.Infrastructure.Persistence;
using BudgetWise.Infrastructure.Persistence.Interceptors;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Wolverine;

namespace BudgetWise.UnitTests.Infrastructure;

/// <summary>
/// End-to-end flow tests for the domain event dispatch pipeline.
///
/// These tests verify the COMPLETE chain:
///   Entity raises event  →  EF Core SaveChanges  →  Interceptor fires
///   →  IMessageBus.PublishAsync called with correct event type and payload
///
/// They are different from DomainEventDispatcherInterceptorTests because
/// they focus on the SEQUENCE of actions across the full stack
/// (aggregate → persistence → messaging), not just the interceptor in isolation.
/// </summary>
public sealed class DomainEventDispatchFlowTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new DomainEventDispatcherInterceptor(_bus));

    // ── Create flow ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_SaveChanges_ShouldDispatchTransactionCreatedEvent()
    {
        // Arrange
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(
            UserId, "Salário", 5000m, TransactionType.Income, Today);

        // Act
        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();

        // Assert — the full chain produced the right event
        await _bus.Received(1).PublishAsync(Arg.Is<TransactionCreatedEvent>(e =>
            e.TransactionId == transaction.Id &&
            e.UserId == UserId &&
            e.Amount == 5000m &&
            e.Type == TransactionType.Income &&
            e.TransactionDate == Today));
    }

    // ── Confirm flow ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Confirm_SaveChanges_ShouldDispatchTransactionConfirmedEvent()
    {
        // Arrange — persist first (clears create event)
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(
            UserId, "Aluguel", 1200m, TransactionType.Expense, Today);
        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();
        _bus.ClearReceivedCalls();

        // Act
        transaction.Confirm();
        await ctx.SaveChangesAsync();

        // Assert
        await _bus.Received(1).PublishAsync(Arg.Is<TransactionConfirmedEvent>(e =>
            e.TransactionId == transaction.Id &&
            e.UserId == UserId));
    }

    // ── SoftDelete flow ───────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDelete_SaveChanges_ShouldDispatchTransactionDeletedEvent()
    {
        // Arrange
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(
            UserId, "Compra", 300m, TransactionType.Expense, Today);
        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();
        _bus.ClearReceivedCalls();

        // Act
        transaction.SoftDelete();
        await ctx.SaveChangesAsync();

        // Assert
        await _bus.Received(1).PublishAsync(Arg.Is<TransactionDeletedEvent>(e =>
            e.TransactionId == transaction.Id &&
            e.UserId == UserId));
    }

    // ── Event ordering ────────────────────────────────────────────────────────

    [Fact]
    public async Task MultipleOperations_SeparateSaves_ShouldDispatchEventsInOrder()
    {
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(
            UserId, "Salário", 5000m, TransactionType.Income, Today);
        ctx.Transactions.Add(transaction);

        // Save 1: TransactionCreatedEvent
        await ctx.SaveChangesAsync();
        await _bus.Received(1).PublishAsync(Arg.Any<TransactionCreatedEvent>());
        _bus.ClearReceivedCalls();

        // Save 2: TransactionConfirmedEvent
        transaction.Confirm();
        await ctx.SaveChangesAsync();
        await _bus.Received(1).PublishAsync(Arg.Any<TransactionConfirmedEvent>());
        _bus.ClearReceivedCalls();

        // Save 3: TransactionDeletedEvent
        transaction.SoftDelete();
        await ctx.SaveChangesAsync();
        await _bus.Received(1).PublishAsync(Arg.Any<TransactionDeletedEvent>());
    }

    // ── No events guard ───────────────────────────────────────────────────────

    [Fact]
    public async Task NoStateChange_SaveChanges_ShouldNotDispatchAnyEvent()
    {
        await using var ctx = CreateContext();

        // No entities tracked, no events
        await ctx.SaveChangesAsync();

        await _bus.DidNotReceive().PublishAsync(Arg.Any<object>());
    }

    // ── Bus failure isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task WhenBusThrows_SaveChanges_ShouldPropagateException()
    {
        // Data was already committed to DB; if the bus fails, the caller should know
        _bus.PublishAsync(Arg.Any<object>())
            .Returns<ValueTask>(x => ValueTask.FromException(new InvalidOperationException("Bus unavailable")));

        await using var ctx = CreateContext();
        var transaction = Transaction.Create(
            UserId, "Salário", 5000m, TransactionType.Income, Today);
        ctx.Transactions.Add(transaction);

        var act = async () => await ctx.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Bus unavailable");
    }
}
