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
/// Tests the DomainEventDispatcherInterceptor in isolation.
///
/// Strategy:
///   - Use EF Core in-memory database so there is no real PostgreSQL dependency
///   - Mock IMessageBus with NSubstitute to capture PublishAsync calls
///   - Verify that events raised inside aggregates are forwarded to the bus
///     exactly once per event, after SaveChangesAsync completes
/// </summary>
public sealed class DomainEventDispatcherInterceptorTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new DomainEventDispatcherInterceptor(_bus));

    // ── Publish contract ─────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenEntityRaisesEvent_ShouldPublishToMessageBus()
    {
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today);
        ctx.Transactions.Add(transaction);

        await ctx.SaveChangesAsync();

        await _bus.Received(1).PublishAsync(Arg.Any<TransactionCreatedEvent>());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityRaisesMultipleEvents_ShouldPublishEachOne()
    {
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today);
        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();
        _bus.ClearReceivedCalls();

        // Confirm raises a second event (TransactionConfirmedEvent)
        transaction.Confirm(Today);
        await ctx.SaveChangesAsync();

        await _bus.Received(1).PublishAsync(Arg.Any<TransactionConfirmedEvent>());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoEventsRaised_ShouldNotCallPublish()
    {
        await using var ctx = CreateContext();

        // SaveChanges with no tracked entities — no events
        await ctx.SaveChangesAsync();

        await _bus.DidNotReceive().PublishAsync(Arg.Any<object>());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenTwoEntitiesRaiseEvents_ShouldPublishBoth()
    {
        await using var ctx = CreateContext();
        var t1 = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today);
        var t2 = Transaction.Create(UserId, "Aluguel", 1200m, TransactionType.Expense, Today);
        ctx.Transactions.AddRange(t1, t2);

        await ctx.SaveChangesAsync();

        // One TransactionCreatedEvent per entity
        await _bus.Received(2).PublishAsync(Arg.Any<TransactionCreatedEvent>());
    }

    // ── Event identity ────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_ShouldPublishEventWithCorrectTransactionId()
    {
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today);
        ctx.Transactions.Add(transaction);

        await ctx.SaveChangesAsync();

        await _bus.Received(1).PublishAsync(
            Arg.Is<TransactionCreatedEvent>(e => e.TransactionId == transaction.Id));
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPublishEventWithCorrectUserId()
    {
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today);
        ctx.Transactions.Add(transaction);

        await ctx.SaveChangesAsync();

        await _bus.Received(1).PublishAsync(
            Arg.Is<TransactionCreatedEvent>(e => e.UserId == UserId));
    }

    // ── PopDomainEvents idempotency ───────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_CalledTwice_ShouldNotRepublishSameEvents()
    {
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(UserId, "Salário", 5000m, TransactionType.Income, Today);
        ctx.Transactions.Add(transaction);

        await ctx.SaveChangesAsync();
        _bus.ClearReceivedCalls();

        // No new state changes — second save should produce no events
        await ctx.SaveChangesAsync();

        await _bus.DidNotReceive().PublishAsync(Arg.Any<object>());
    }

    [Fact]
    public async Task SaveChangesAsync_AfterPopDomainEvents_ShouldHaveEmptyEventList()
    {
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(UserId, "Test", 100m, TransactionType.Expense, Today);
        ctx.Transactions.Add(transaction);

        await ctx.SaveChangesAsync();

        // After save, PopDomainEvents was called by the interceptor — collection must be empty
        transaction.DomainEvents.Should().BeEmpty();
    }

    // ── SoftDelete event ──────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenTransactionSoftDeleted_ShouldPublishDeletedEvent()
    {
        await using var ctx = CreateContext();
        var transaction = Transaction.Create(UserId, "Compra", 200m, TransactionType.Expense, Today);
        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();
        _bus.ClearReceivedCalls();

        transaction.SoftDelete();
        await ctx.SaveChangesAsync();

        await _bus.Received(1).PublishAsync(
            Arg.Is<TransactionDeletedEvent>(e =>
                e.TransactionId == transaction.Id &&
                e.UserId == UserId));
    }

    // ── FamilyGroup aggregate events ──────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenFamilyGroupCreated_ShouldTrackWithoutEvents()
    {
        // FamilyGroup.Create does not raise events — it is a creation without side effects.
        // Navigation-based events (AddMember) require real DB and are covered by integration tests.
        await using var ctx = CreateContext();
        var group = FamilyGroup.Create(Guid.NewGuid(), "Família Silva");
        ctx.FamilyGroups.Add(group);

        await ctx.SaveChangesAsync();

        // No events were raised by FamilyGroup.Create
        await _bus.DidNotReceive().PublishAsync(Arg.Any<FamilyMemberJoinedEvent>());
    }
}
