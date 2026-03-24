using BudgetWise.Domain.Events;
using Microsoft.Extensions.Logging;

namespace BudgetWise.Application.Transactions.EventHandlers;

/// <summary>
/// Handles the <see cref="TransactionCreatedEvent"/> domain event.
/// Example: update a running budget balance, trigger a budget-alert check, etc.
/// </summary>
internal sealed class TransactionCreatedHandler(ILogger<TransactionCreatedHandler> logger)
{
    public Task HandleAsync(TransactionCreatedEvent ev, CancellationToken ct)
    {
        logger.LogInformation(
            "Transaction {TransactionId} created — amount: {Amount} ({Type}) on {Date}",
            ev.TransactionId,
            ev.Amount,
            ev.Type,
            ev.TransactionDate);

        // Future: recalculate monthly budget, check spending limits, etc.
        return Task.CompletedTask;
    }
}
