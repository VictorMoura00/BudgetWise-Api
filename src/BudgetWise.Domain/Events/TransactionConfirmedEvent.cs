using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Events;

public sealed record TransactionConfirmedEvent(
    Guid TransactionId,
    Guid UserId) : IDomainEvent;
