using BudgetWise.Domain.Events;
using Microsoft.Extensions.Logging;

namespace BudgetWise.Application.Transactions.EventHandlers;

/// <summary>
/// Handles the <see cref="TransactionConfirmedEvent"/> domain event.
///
/// Wolverine discovers this handler automatically because:
///   1. The class is in an assembly registered via opts.Discovery.IncludeAssembly()
///   2. The method is named "Handle" and accepts a known event type as the first parameter
///
/// To add side effects (email, push notification, audit log), inject the needed
/// service into the constructor — Wolverine resolves from the DI container.
/// </summary>
internal sealed class TransactionConfirmedHandler(ILogger<TransactionConfirmedHandler> logger)
{
    public Task HandleAsync(TransactionConfirmedEvent ev, CancellationToken ct)
    {
        logger.LogInformation(
            "Transaction {TransactionId} confirmed for user {UserId}",
            ev.TransactionId,
            ev.UserId);

        // Future: send push notification, update budget summary cache, etc.
        return Task.CompletedTask;
    }
}
