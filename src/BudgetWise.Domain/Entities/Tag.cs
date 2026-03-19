using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Entities;

public class Tag : Entity
{
    public Guid UserId { get; private init; }
    public string Name { get; private set; } = string.Empty;

    // Navegação
    public ICollection<TransactionTag> TransactionTags { get; private init; } = [];

    private Tag() { } // EF Core

    public static Tag Create(Guid userId, string name)
    {
        return new Tag
        {
            UserId = userId,
            Name = name.Trim().ToLowerInvariant()
        };
    }

    public void Rename(string newName)
    {
        Name = newName.Trim().ToLowerInvariant();
    }
}