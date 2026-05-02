using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Exceptions;

namespace BudgetWise.Domain.Entities;

public class FamilyMember : Entity
{
    public Guid FamilyGroupId { get; private init; }
    public Guid UserId { get; private init; }
    public FamilyMemberRole Role { get; private set; }
    public DateTime JoinedAt { get; private init; } = DateTime.UtcNow;
    public FamilyGroup FamilyGroup { get; private init; } = null!;

    private FamilyMember() { }

    public static FamilyMember CreateOwner(Guid familyGroupId, Guid userId)
    {
        return new FamilyMember
        {
            FamilyGroupId = familyGroupId,
            UserId = userId,
            Role = FamilyMemberRole.Owner
        };
    }

    public static FamilyMember CreateMember(Guid familyGroupId, Guid userId)
    {
        return new FamilyMember
        {
            Id = Guid.Empty,
            FamilyGroupId = familyGroupId,
            UserId = userId,
            Role = FamilyMemberRole.Member
        };
    }

    /// <summary>
    /// Changes the member's role. The Owner role cannot be assigned this way —
    /// ownership transfer is an explicit operation handled at the group level.
    /// </summary>
    public void ChangeRole(FamilyMemberRole newRole)
    {
        if (newRole == FamilyMemberRole.Owner)
            throw new DomainException("Ownership transfer must be done explicitly via the group.");

        if (Role == FamilyMemberRole.Owner)
            throw new DomainException("The group owner's role cannot be changed.");

        Role = newRole;
        SetUpdated();
    }
}
