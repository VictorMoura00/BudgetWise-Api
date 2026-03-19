namespace BudgetWise.Domain.Common.Abstractions;

/// <summary>
/// Interface para entidades com soft delete via deleted_at.
/// Aplicada apenas em Transaction e Category.
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; }
    bool IsDeleted => DeletedAt is not null;
    void SoftDelete();
}