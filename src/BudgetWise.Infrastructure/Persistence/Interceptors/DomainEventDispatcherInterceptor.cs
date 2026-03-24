using BudgetWise.Domain.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Wolverine;

namespace BudgetWise.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that dispatches domain events via Wolverine
/// after the transaction is committed to the database.
///
/// Flow:
///   Use Case → SaveChangesAsync() → [DB commit] → SavedChangesAsync()
///              → PopDomainEvents() from all tracked aggregates
///              → IMessageBus.PublishAsync(event) for each event
///              → Wolverine routes to registered handlers
///
/// Events are dispatched AFTER the commit — this guarantees consistency:
/// handlers see data that is already persisted.
/// </summary>
public sealed class DomainEventDispatcherInterceptor(IMessageBus bus) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        // Sync path: fire-and-forget (rare — prefer async callers)
        DispatchDomainEventsAsync(eventData.Context, CancellationToken.None)
            .GetAwaiter().GetResult();
        return result;
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null) return;

        // Collect events from every tracked Entity that has pending domain events
        var events = context.ChangeTracker
            .Entries<Entity>()
            .SelectMany(entry => entry.Entity.PopDomainEvents())
            .ToList();

        if (events.Count == 0) return;

        // PublishAsync routes each event to all registered Wolverine handlers.
        // Because we're inside a web request, Wolverine inherits the ambient
        // IServiceScope — handlers can safely use scoped services (DbContext, repos).
        foreach (var domainEvent in events)
            await bus.PublishAsync(domainEvent);
    }
}
