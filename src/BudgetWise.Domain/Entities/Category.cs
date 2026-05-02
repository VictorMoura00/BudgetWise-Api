using BudgetWise.Domain.Common.Abstractions;
using BudgetWise.Domain.Enums;
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
    public CategoryType CategoryType { get; private set; } = CategoryType.Both;
    public Guid? UserId { get; private init; }

    public ICollection<Transaction> Transactions { get; private init; } = [];

    private Category() { }

    public static Category CreateSystem(
        string name,
        CategoryType categoryType,
        string? icon = null,
        HexColor? color = null,
        string? description = null)
    {
        return new Category
        {
            Name = name,
            CategoryType = categoryType,
            Icon = icon,
            Color = color,
            Description = description,
            IsSystem = true,
            UserId = null
        };
    }

    public static Category CreatePersonal(
        Guid userId,
        string name,
        string? description = null,
        string? icon = null,
        HexColor? color = null,
        CategoryType categoryType = CategoryType.Both)
    {
        return new Category
        {
            Name = name,
            Description = description,
            Icon = icon,
            Color = color,
            CategoryType = categoryType,
            IsSystem = false,
            UserId = userId
        };
    }

    public void Update(string name, string? description, string? icon, HexColor? color, CategoryType categoryType)
    {
        Name = name;
        Description = description;
        Icon = icon;
        Color = color;
        CategoryType = categoryType;
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
