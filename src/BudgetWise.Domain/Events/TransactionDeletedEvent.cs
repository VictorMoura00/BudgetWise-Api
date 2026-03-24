using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Events;

public sealed record TransactionDeletedEvent(
    Guid TransactionId,
    Guid UserId,
    DateTime DeletedAt) : IDomainEvent;
