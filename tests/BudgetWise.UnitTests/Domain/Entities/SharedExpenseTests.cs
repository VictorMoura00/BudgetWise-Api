using BudgetWise.Domain.Entities;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class SharedExpenseTests
{
    private static readonly Guid TransactionId = Guid.NewGuid();
    private static readonly Guid FamilyGroupId = Guid.NewGuid();
    private static readonly Guid CreatedBy = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 300m, CreatedBy, "Jantar em família");

        expense.TransactionId.Should().Be(TransactionId);
        expense.FamilyGroupId.Should().Be(FamilyGroupId);
        expense.TotalAmount.Should().Be(300m);
        expense.CreatedBy.Should().Be(CreatedBy);
        expense.Description.Should().Be("Jantar em família");
        expense.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithNullDescription_ShouldSetNullDescription()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 100m, CreatedBy);

        expense.Description.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyParticipantsCollection()
    {
        var expense = SharedExpense.Create(TransactionId, FamilyGroupId, 100m, CreatedBy);

        expense.Participants.Should().BeEmpty();
    }
}
