namespace BudgetWise.Domain.Common.Abstractions;

/// <summary>
/// Marker interface for domain events dispatched via Wolverine after SaveChangesAsync.
/// Events represent facts that have already happened inside an aggregate.
/// </summary>
public interface IDomainEvent;
