namespace BudgetWise.Domain.Common.Abstractions;

/// <summary>
/// Classe base para todas as entidades do domínio.
/// Igualdade por identidade (Id), não por valor.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Returns all pending events and clears the internal collection.
    /// Called by the infrastructure after SaveChangesAsync to dispatch via Wolverine.
    /// </summary>
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList().AsReadOnly();
        _domainEvents.Clear();
        return events;
    }

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