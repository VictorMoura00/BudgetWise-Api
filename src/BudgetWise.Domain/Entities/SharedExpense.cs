using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Events;
using BudgetWise.Domain.Exceptions;

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
        if (totalAmount <= 0)
            throw new DomainException("Total amount must be greater than zero.");

        return new SharedExpense
        {
            TransactionId = transactionId,
            FamilyGroupId = familyGroupId,
            TotalAmount = totalAmount,
            CreatedBy = createdBy,
            Description = description
        };
    }

    /// <summary>
    /// Adds a participant with their share of the expense.
    /// Validates that the running total of assigned amounts does not exceed <see cref="TotalAmount"/>.
    /// Raises <see cref="SharedExpenseFullySettledEvent"/> when all participants have settled.
    /// </summary>
    public SharedExpenseParticipant AddParticipant(Guid userId, decimal amountOwed)
    {
        if (amountOwed <= 0)
            throw new DomainException("Amount owed must be greater than zero.");

        if (Participants.Any(p => p.UserId == userId))
            throw new DomainException("User is already a participant in this shared expense.");

        var assigned = Participants.Sum(p => p.AmountOwed);
        if (assigned + amountOwed > TotalAmount)
            throw new DomainException(
                $"Cannot assign {amountOwed:C}. Only {TotalAmount - assigned:C} remains unassigned.");

        var participant = SharedExpenseParticipant.Create(Id, userId, amountOwed);
        Participants.Add(participant);
        SetUpdated();

        return participant;
    }

    /// <summary>
    /// Called by <see cref="SharedExpenseParticipant.Settle"/> side-effect check.
    /// Raises the fully-settled event when every participant has paid.
    /// </summary>
    public void CheckAndRaiseFullySettled()
    {
        if (Participants.Count > 0 && Participants.All(p => p.IsSettled))
            Raise(new SharedExpenseFullySettledEvent(Id, FamilyGroupId));
    }
}
