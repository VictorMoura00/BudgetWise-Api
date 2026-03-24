using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Exceptions;
using FluentAssertions;

namespace BudgetWise.UnitTests.Domain.Entities;

public sealed class TransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Create_WithRequiredData_ShouldSetAllProperties()
    {
        var transaction = Transaction.Create(
            userId: UserId,
            description: "Salário",
            amount: 5000m,
            type: TransactionType.Income,
            transactionDate: Today);

        transaction.UserId.Should().Be(UserId);
        transaction.Description.Should().Be("Salário");
        transaction.Amount.Should().Be(5000m);
        transaction.Type.Should().Be(TransactionType.Income);
        transaction.TransactionDate.Should().Be(Today);
        transaction.RecurrenceType.Should().Be(RecurrenceType.None);
        transaction.IsConfirmed.Should().BeFalse();
        transaction.DeletedAt.Should().BeNull();
        transaction.CategoryId.Should().BeNull();
        transaction.Notes.Should().BeNull();
        transaction.PaymentMethod.Should().BeNull();
        transaction.FamilyGroupId.Should().BeNull();
        transaction.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithAllOptionalData_ShouldSetAllProperties()
    {
        var categoryId = Guid.NewGuid();
        var familyGroupId = Guid.NewGuid();
        var recurrenceEnd = Today.AddMonths(6);

        var transaction = Transaction.Create(
            userId: UserId,
            description: "Aluguel",
            amount: 1200m,
            type: TransactionType.Expense,
            transactionDate: Today,
            categoryId: categoryId,
            notes: "Parcela 1/12",
            recurrenceType: RecurrenceType.Monthly,
            recurrenceEndDate: recurrenceEnd,
            isConfirmed: true,
            paymentMethod: PaymentMethod.Pix,
            familyGroupId: familyGroupId);

        transaction.CategoryId.Should().Be(categoryId);
        transaction.Notes.Should().Be("Parcela 1/12");
        transaction.RecurrenceType.Should().Be(RecurrenceType.Monthly);
        transaction.RecurrenceEndDate.Should().Be(recurrenceEnd);
        transaction.IsConfirmed.Should().BeTrue();
        transaction.PaymentMethod.Should().Be(PaymentMethod.Pix);
        transaction.FamilyGroupId.Should().Be(familyGroupId);
    }

    [Fact]
    public void Update_WhenNotDeleted_ShouldUpdateAllProperties()
    {
        var transaction = Transaction.Create(UserId, "Original", 100m, TransactionType.Expense, Today);
        var newCategoryId = Guid.NewGuid();
        var before = transaction.UpdatedAt;

        transaction.Update(
            description: "Atualizado",
            amount: 200m,
            type: TransactionType.Income,
            transactionDate: Today.AddDays(1),
            categoryId: newCategoryId,
            notes: "Nota",
            recurrenceType: RecurrenceType.Weekly,
            recurrenceEndDate: Today.AddMonths(1),
            paymentMethod: PaymentMethod.CreditCard,
            familyGroupId: null);

        transaction.Description.Should().Be("Atualizado");
        transaction.Amount.Should().Be(200m);
        transaction.Type.Should().Be(TransactionType.Income);
        transaction.CategoryId.Should().Be(newCategoryId);
        transaction.Notes.Should().Be("Nota");
        transaction.RecurrenceType.Should().Be(RecurrenceType.Weekly);
        transaction.PaymentMethod.Should().Be(PaymentMethod.CreditCard);
        transaction.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Update_WhenDeleted_ShouldThrowDomainException()
    {
        var transaction = Transaction.Create(UserId, "Compra", 50m, TransactionType.Expense, Today);
        transaction.SoftDelete();

        var act = () => transaction.Update("Nova", 60m, TransactionType.Expense, Today, null, null, RecurrenceType.None, null, null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*deleted*");
    }

    [Fact]
    public void Confirm_WhenNotDeleted_ShouldSetIsConfirmedTrue()
    {
        var transaction = Transaction.Create(UserId, "Compra", 100m, TransactionType.Expense, Today);

        transaction.Confirm();

        transaction.IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public void Confirm_WhenDeleted_ShouldThrowDomainException()
    {
        var transaction = Transaction.Create(UserId, "Compra", 100m, TransactionType.Expense, Today);
        transaction.SoftDelete();

        var act = () => transaction.Confirm();

        act.Should().Throw<DomainException>()
            .WithMessage("*deleted*");
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedAt()
    {
        var transaction = Transaction.Create(UserId, "Compra", 100m, TransactionType.Expense, Today);

        transaction.SoftDelete();

        transaction.DeletedAt.Should().NotBeNull();
        transaction.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void IsDeleted_WhenDeletedAtIsNull_ShouldReturnFalse()
    {
        var transaction = Transaction.Create(UserId, "Compra", 100m, TransactionType.Expense, Today);

        transaction.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_AfterSoftDelete_ShouldReturnTrue()
    {
        var transaction = Transaction.Create(UserId, "Compra", 100m, TransactionType.Expense, Today);
        transaction.SoftDelete();

        transaction.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyCollections()
    {
        var transaction = Transaction.Create(UserId, "Test", 1m, TransactionType.Income, Today);

        transaction.TransactionTags.Should().BeEmpty();
        transaction.SharedExpenses.Should().BeEmpty();
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldRemainConfirmed()
    {
        var transaction = Transaction.Create(UserId, "Compra", 100m, TransactionType.Expense, Today, isConfirmed: true);

        transaction.Confirm();

        transaction.IsConfirmed.Should().BeTrue();
    }
}
