using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Entities;

public class SharedExpense : Entity, IAggregateRoot
{
    public Guid TransactionId { get; private init; }
    public Guid FamilyGroupId { get; private init; }
    public string? Description { get; private set; }
    public decimal TotalAmount { get; private init; }
    public Guid CreatedBy { get; private init; }
    public Transaction Transaction { get; private init; } = null!;
    public FamilyGroup FamilyGroup { get; private init; } = null!;
    public ICollection<SharedExpenseParticipant> Participants { get; private init; } = [];

    private SharedExpense() { } 

    public static SharedExpense Create(
        Guid transactionId,
        Guid familyGroupId,
        decimal totalAmount,
        Guid createdBy,
        string? description = null)
    {
        return new SharedExpense
        {
            TransactionId = transactionId,
            FamilyGroupId = familyGroupId,
            TotalAmount = totalAmount,
            CreatedBy = createdBy,
            Description = description
        };
    }
}
