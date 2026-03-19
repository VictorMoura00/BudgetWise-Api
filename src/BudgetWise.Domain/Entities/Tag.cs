using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Entities;

public class Tag : Entity
{
    public Guid UserId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public ICollection<TransactionTag> TransactionTags { get; private init; } = [];

    private Tag() { } 

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