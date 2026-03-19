using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Entities;

public class FamilyGroup : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string InviteCode { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private init; }

    // Navegação
    public ICollection<FamilyMember> Members { get; private init; } = [];
    public ICollection<Transaction> Transactions { get; private init; } = [];
    public ICollection<SharedExpense> SharedExpenses { get; private init; } = [];

    private FamilyGroup() { } // EF Core

    public static FamilyGroup Create(Guid createdBy, string name, string? description = null)
    {
        return new FamilyGroup
        {
            Name = name,
            Description = description,
            InviteCode = GenerateInviteCode(),
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        SetUpdated();
    }

    public void RegenerateInviteCode()
    {
        InviteCode = GenerateInviteCode();
        SetUpdated();
    }

    private static string GenerateInviteCode()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    }
}
