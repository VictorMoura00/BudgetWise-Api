using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Enums;

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
            FamilyGroupId = familyGroupId,
            UserId = userId,
            Role = FamilyMemberRole.Member
        };
    }
}
