using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Events;

public sealed record SharedExpenseFullySettledEvent(
    Guid SharedExpenseId,
    Guid FamilyGroupId) : IDomainEvent;
