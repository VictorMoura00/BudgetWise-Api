using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Enums;

namespace BudgetWise.Domain.Events;

public sealed record FamilyMemberJoinedEvent(
    Guid FamilyGroupId,
    Guid UserId,
    FamilyMemberRole Role) : IDomainEvent;
