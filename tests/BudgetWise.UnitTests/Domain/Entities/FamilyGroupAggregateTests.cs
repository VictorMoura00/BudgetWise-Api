using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Exceptions;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class FamilyGroupAggregateTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public void AddMember_WithNewUser_ShouldAddToMembersCollection()
    {
        var group = FamilyGroup.Create(OwnerId, "Família");
        var userId = Guid.NewGuid();

        group.AddMember(userId);

        group.Members.Should().ContainSingle(m => m.UserId == userId);
    }

    [Fact]
    public void AddMember_ShouldReturnMemberWithCorrectRole()
    {
        var group = FamilyGroup.Create(OwnerId, "Família");
        var userId = Guid.NewGuid();

        var member = group.AddMember(userId);

        member.Role.Should().Be(FamilyMemberRole.Member);
        member.UserId.Should().Be(userId);
        member.FamilyGroupId.Should().Be(group.Id);
    }

    [Fact]
    public void AddMember_WhenAlreadyMember_ShouldThrowDomainException()
    {
        var group = FamilyGroup.Create(OwnerId, "Família");
        var userId = Guid.NewGuid();
        group.AddMember(userId);

        var act = () => group.AddMember(userId);

        act.Should().Throw<DomainException>()
            .WithMessage("*already a member*");
    }

    [Fact]
    public void AddMember_ShouldUpdateUpdatedAt()
    {
        var group = FamilyGroup.Create(OwnerId, "Família");
        var before = group.UpdatedAt;

        group.AddMember(Guid.NewGuid());

        group.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void RemoveMember_WithExistingMember_ShouldRemoveFromCollection()
    {
        var group = FamilyGroup.Create(OwnerId, "Família");
        var userId = Guid.NewGuid();
        group.AddMember(userId);

        group.RemoveMember(userId, OwnerId);

        group.Members.Should().NotContain(m => m.UserId == userId);
    }

    [Fact]
    public void RemoveMember_WhenRemovingOwner_ShouldThrowDomainException()
    {
        var group = FamilyGroup.Create(OwnerId, "Família");

        var act = () => group.RemoveMember(OwnerId, OwnerId);

        act.Should().Throw<DomainException>()
            .WithMessage("*owner cannot be removed*");
    }

    [Fact]
    public void RemoveMember_WhenUserNotMember_ShouldThrowDomainException()
    {
        var group = FamilyGroup.Create(OwnerId, "Família");
        var nonMemberId = Guid.NewGuid();

        var act = () => group.RemoveMember(nonMemberId, OwnerId);

        act.Should().Throw<DomainException>()
            .WithMessage("*not a member*");
    }

    [Fact]
    public void RemoveMember_ShouldUpdateUpdatedAt()
    {
        var group = FamilyGroup.Create(OwnerId, "Família");
        var userId = Guid.NewGuid();
        group.AddMember(userId);
        var before = group.UpdatedAt;

        group.RemoveMember(userId, OwnerId);

        group.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
