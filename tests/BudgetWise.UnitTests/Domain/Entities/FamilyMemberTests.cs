using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class FamilyMemberTests
{
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void CreateOwner_ShouldSetRoleToOwner()
    {
        var member = FamilyMember.CreateOwner(GroupId, UserId);

        member.Role.Should().Be(FamilyMemberRole.Owner);
        member.FamilyGroupId.Should().Be(GroupId);
        member.UserId.Should().Be(UserId);
        member.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateMember_ShouldSetRoleToMember()
    {
        var member = FamilyMember.CreateMember(GroupId, UserId);

        member.Role.Should().Be(FamilyMemberRole.Member);
        member.FamilyGroupId.Should().Be(GroupId);
        member.UserId.Should().Be(UserId);
    }

    [Fact]
    public void CreateOwner_ShouldSetJoinedAtToNow()
    {
        var member = FamilyMember.CreateOwner(GroupId, UserId);

        member.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CreateMember_ShouldSetJoinedAtToNow()
    {
        var member = FamilyMember.CreateMember(GroupId, UserId);

        member.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
