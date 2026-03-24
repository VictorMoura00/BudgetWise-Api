using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Exceptions;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class FamilyMemberRoleTests
{
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void ChangeRole_FromMemberToMember_ShouldKeepRole()
    {
        var member = FamilyMember.CreateMember(GroupId, UserId);

        member.ChangeRole(FamilyMemberRole.Member);

        member.Role.Should().Be(FamilyMemberRole.Member);
    }

    [Fact]
    public void ChangeRole_ShouldUpdateUpdatedAt()
    {
        var member = FamilyMember.CreateMember(GroupId, UserId);
        var before = member.UpdatedAt;

        member.ChangeRole(FamilyMemberRole.Member);

        member.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void ChangeRole_ToOwner_ShouldThrowDomainException()
    {
        var member = FamilyMember.CreateMember(GroupId, UserId);

        var act = () => member.ChangeRole(FamilyMemberRole.Owner);

        act.Should().Throw<DomainException>()
            .WithMessage("*Ownership transfer*");
    }

    [Fact]
    public void ChangeRole_WhenMemberIsOwner_ShouldThrowDomainException()
    {
        var owner = FamilyMember.CreateOwner(GroupId, UserId);

        var act = () => owner.ChangeRole(FamilyMemberRole.Member);

        act.Should().Throw<DomainException>()
            .WithMessage("*owner's role cannot be changed*");
    }
}
