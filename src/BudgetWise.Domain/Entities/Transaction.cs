using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Enums;

namespace BudgetWise.Domain.Entities;

public class Transaction : Entity, ISoftDeletable
{
    public Guid UserId { get; private init; }
    public Guid? FamilyGroupId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string? Notes { get; private set; }
    public RecurrenceType RecurrenceType { get; private set; } = RecurrenceType.None;
    public DateOnly? RecurrenceEndDate { get; private set; }
    public bool IsConfirmed { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Category? Category { get; private init; }
    public FamilyGroup? FamilyGroup { get; private init; }
    public ICollection<TransactionTag> TransactionTags { get; private init; } = [];
    public ICollection<SharedExpense> SharedExpenses { get; private init; } = [];

    private Transaction() { }

    public static Transaction Create(
        Guid userId,
        string description,
        decimal amount,
        TransactionType type,
        DateOnly transactionDate,
        Guid? categoryId = null,
        string? notes = null,
        RecurrenceType recurrenceType = RecurrenceType.None,
        DateOnly? recurrenceEndDate = null,
        bool isConfirmed = false,
        PaymentMethod? paymentMethod = null,
        Guid? familyGroupId = null)
    {
        return new Transaction
        {
            UserId = userId,
            Description = description,
            Amount = amount,
            Type = type,
            TransactionDate = transactionDate,
            CategoryId = categoryId,
            Notes = notes,
            RecurrenceType = recurrenceType,
            RecurrenceEndDate = recurrenceEndDate,
            IsConfirmed = isConfirmed,
            PaymentMethod = paymentMethod,
            FamilyGroupId = familyGroupId
        };
    }

    public void Update(
        string description,
        decimal amount,
        TransactionType type,
        DateOnly transactionDate,
        Guid? categoryId,
        string? notes,
        RecurrenceType recurrenceType,
        DateOnly? recurrenceEndDate,
        PaymentMethod? paymentMethod,
        Guid? familyGroupId)
    {
        if (IsDeleted)
            throw new InvalidOperationException("It is not possible to edit a deleted transaction.");

        Description = description;
        Amount = amount;
        Type = type;
        TransactionDate = transactionDate;
        CategoryId = categoryId;
        Notes = notes;
        RecurrenceType = recurrenceType;
        RecurrenceEndDate = recurrenceEndDate;
        PaymentMethod = paymentMethod;
        FamilyGroupId = familyGroupId;
        SetUpdated();
    }

    public void Confirm()
    {
        if (IsDeleted)
            throw new InvalidOperationException("It is not possible to confirm a deleted transaction.");

        IsConfirmed = true;
        SetUpdated();
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        SetUpdated();
    }

    public bool IsDeleted => DeletedAt is not null;
}