using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Tags.UseCases;

public sealed class GetTagsUseCase(ITagRepository repository) : IUseCase
{
    public async Task<Result<IReadOnlyList<TagResponse>>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tags = await repository.GetAllForUserAsync(userId, cancellationToken);

        return tags.Select(t => new TagResponse(t.Id, t.Name, t.CreatedAt, t.UpdatedAt))
                   .ToList()
                   .AsReadOnly();
    }
}
