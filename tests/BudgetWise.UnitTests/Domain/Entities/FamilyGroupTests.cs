using BudgetWise.Domain.Entities;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class FamilyGroupTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSetNameAndCreatedBy()
    {
        var group = FamilyGroup.Create(OwnerId, "Família Silva", "Grupo principal");

        group.Name.Should().Be("Família Silva");
        group.Description.Should().Be("Grupo principal");
        group.CreatedBy.Should().Be(OwnerId);
        group.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldGenerateNonEmptyInviteCode()
    {
        var group = FamilyGroup.Create(OwnerId, "Test");

        group.InviteCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ShouldGenerateInviteCodeOfTenChars()
    {
        var group = FamilyGroup.Create(OwnerId, "Test");

        group.InviteCode.Should().HaveLength(10);
    }

    [Fact]
    public void Create_ShouldGenerateUppercaseInviteCode()
    {
        var group = FamilyGroup.Create(OwnerId, "Test");

        group.InviteCode.Should().MatchRegex(@"^[0-9A-F]{10}$");
    }

    [Fact]
    public void Create_WithNullDescription_ShouldSetNullDescription()
    {
        var group = FamilyGroup.Create(OwnerId, "Test");

        group.Description.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldChangeName()
    {
        var group = FamilyGroup.Create(OwnerId, "Old");
        var before = group.UpdatedAt;

        group.Update("New", "Nova descrição");

        group.Name.Should().Be("New");
        group.Description.Should().Be("Nova descrição");
        group.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void RegenerateInviteCode_ShouldProduceValidCode()
    {
        var group = FamilyGroup.Create(OwnerId, "Test");

        group.RegenerateInviteCode();

        group.InviteCode.Should().HaveLength(10);
        group.InviteCode.Should().MatchRegex(@"^[0-9A-F]{10}$");
    }

    [Fact]
    public void RegenerateInviteCode_ShouldUpdateUpdatedAt()
    {
        var group = FamilyGroup.Create(OwnerId, "Test");
        var before = group.UpdatedAt;

        group.RegenerateInviteCode();

        group.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Create_ShouldInitializeEmptyCollections()
    {
        var group = FamilyGroup.Create(OwnerId, "Test");

        group.Members.Should().BeEmpty();
        group.Transactions.Should().BeEmpty();
        group.SharedExpenses.Should().BeEmpty();
    }
}
