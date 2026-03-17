using System;

namespace BudgetWise.Domain.Common.Abstractions;

public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; } = false;

    protected void SetUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        SetUpdated();
    }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => Id.GetHashCode();
}