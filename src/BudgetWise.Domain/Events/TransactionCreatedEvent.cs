using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Enums;

namespace BudgetWise.Domain.Events;

public sealed record TransactionCreatedEvent(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    TransactionType Type,
    DateOnly TransactionDate) : IDomainEvent;
