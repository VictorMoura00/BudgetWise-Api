using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Events;

public sealed record FamilyMemberRemovedEvent(
    Guid FamilyGroupId,
    Guid RemovedUserId,
    Guid RemovedByUserId) : IDomainEvent;
