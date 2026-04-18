using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Exceptions;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Domain.Entities;

public class Category : Entity, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Icon { get; private set; }
    public HexColor? Color { get; private set; }
    public bool IsSystem { get; private init; }
    public bool IsActive { get; private set; } = true;
    public Guid? UserId { get; private init; }

    public ICollection<Transaction> Transactions { get; private init; } = [];

    private Category() { }

    public static Category CreateSystem(string name, string? icon = null, HexColor? color = null, string? description = null)
    {
        return new Category
        {
            Name = name,
            Icon = icon,
            Color = color,
            Description = description,
            IsSystem = true,
            UserId = null
        };
    }

    public static Category CreatePersonal(Guid userId, string name, string? description = null, string? icon = null, HexColor? color = null)
    {
        return new Category
        {
            Name = name,
            Description = description,
            Icon = icon,
            Color = color,
            IsSystem = false,
            UserId = userId
        };
    }

    public void Update(string name, string? description, string? icon, HexColor? color)
    {
        Name = name;
        Description = description;
        Icon = icon;
        Color = color;
        SetUpdated();
    }

    public void Deactivate()
    {
        if (IsSystem)
            throw new DomainException("System categories cannot be deactivated.");

        IsActive = false;
        SetUpdated();
    }

    public void Activate()
    {
        if (IsSystem)
            throw new DomainException("System categories do not need to be manually activated.");

        IsActive = true;
        SetUpdated();
    }
}
