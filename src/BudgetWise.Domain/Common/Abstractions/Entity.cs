namespace BudgetWise.Domain.Common.Abstractions;

/// <summary>
/// Classe base para todas as entidades do domínio.
/// Igualdade por identidade (Id), não por valor.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);
    public override int GetHashCode() => Id.GetHashCode();
}