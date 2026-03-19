using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Entities;

public class SharedExpenseParticipant : Entity
{
    public Guid SharedExpenseId { get; private init; }
    public Guid UserId { get; private init; }
    public decimal AmountOwed { get; private init; }
    public bool IsSettled { get; private set; }
    public DateTime? SettledAt { get; private set; }
    public SharedExpense SharedExpense { get; private init; } = null!;

    private SharedExpenseParticipant() { }

    public static SharedExpenseParticipant Create(Guid sharedExpenseId, Guid userId, decimal amountOwed)
    {
        return new SharedExpenseParticipant
        {
            SharedExpenseId = sharedExpenseId,
            UserId = userId,
            AmountOwed = amountOwed
        };
    }

    public void Settle()
    {
        if (IsSettled)
            throw new InvalidOperationException("This expense has already been paid off.");

        IsSettled = true;
        SettledAt = DateTime.UtcNow;
        SetUpdated();
    }
}
