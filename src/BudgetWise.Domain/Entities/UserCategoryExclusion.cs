using BudgetWise.Domain.Common.Abstractions;

namespace BudgetWise.Domain.Entities;

public class UserCategoryExclusion : Entity
{
    public Guid UserId { get; private init; }
    public Guid CategoryId { get; private init; }

    private UserCategoryExclusion() { }

    public static UserCategoryExclusion Create(Guid userId, Guid categoryId) => new()
    {
        UserId = userId,
        CategoryId = categoryId
    };
}
