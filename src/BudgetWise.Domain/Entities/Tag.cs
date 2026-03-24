using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Exceptions;

namespace BudgetWise.Domain.Entities;

public class Tag : Entity, IAggregateRoot
{
    public Guid UserId { get; private init; }
    public string Name { get; private set; } = string.Empty;
    public ICollection<TransactionTag> TransactionTags { get; private init; } = [];

    private Tag() { }

    public static Tag Create(Guid userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tag name cannot be empty.");

        return new Tag
        {
            UserId = userId,
            Name = name.Trim().ToLowerInvariant()
        };
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Tag name cannot be empty.");

        Name = newName.Trim().ToLowerInvariant();
        SetUpdated();
    }
}
