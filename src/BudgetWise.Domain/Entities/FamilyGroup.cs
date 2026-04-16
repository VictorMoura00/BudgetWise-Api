using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Events;
using BudgetWise.Domain.Exceptions;

namespace BudgetWise.Domain.Entities;

public class FamilyGroup : Entity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string InviteCode { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private init; }
    public ICollection<FamilyMember> Members { get; private init; } = [];
    public ICollection<Transaction> Transactions { get; private init; } = [];
    public ICollection<SharedExpense> SharedExpenses { get; private init; } = [];

    private FamilyGroup() { }

    public static FamilyGroup Create(Guid createdBy, string name, string? description = null)
    {
        var group = new FamilyGroup
        {
            Name = name,
            Description = description,
            InviteCode = GenerateInviteCode(),
            CreatedBy = createdBy
        };

        var owner = FamilyMember.CreateOwner(group.Id, createdBy);
        group.Members.Add(owner);

        return group;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        SetUpdated();
    }

    public void RegenerateInviteCode()
    {
        InviteCode = GenerateInviteCode();
        SetUpdated();
    }

    /// <summary>
    /// Adds a new member to the group via invite code flow.
    /// Raises <see cref="FamilyMemberJoinedEvent"/>.
    /// </summary>
    public FamilyMember AddMember(Guid userId)
    {
        if (Members.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this family group.");

        var member = FamilyMember.CreateMember(Id, userId);
        Members.Add(member);
        SetUpdated();

        Raise(new FamilyMemberJoinedEvent(Id, userId, FamilyMemberRole.Member));

        return member;
    }

    /// <summary>
    /// Removes a member from the group. The owner cannot be removed.
    /// Raises <see cref="FamilyMemberRemovedEvent"/>.
    /// </summary>
    public void RemoveMember(Guid userId, Guid removedByUserId)
    {
        if (userId == CreatedBy)
            throw new DomainException("The group owner cannot be removed.");

        var member = Members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new DomainException("User is not a member of this family group.");

        Members.Remove(member);
        SetUpdated();

        Raise(new FamilyMemberRemovedEvent(Id, userId, removedByUserId));
    }

    private static string GenerateInviteCode()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    }
}
