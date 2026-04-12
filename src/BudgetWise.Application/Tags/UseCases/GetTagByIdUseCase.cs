using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.Common;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Tags.UseCases;

public sealed class GetTagByIdUseCase(ITagRepository repository) : IUseCase
{
    public async Task<Result<TagResponse>> ExecuteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tag = await repository.GetByIdForUserAsync(id, userId, cancellationToken);

        if (tag is null)
            return TagErrors.NotFound(id);

        return new TagResponse(tag.Id, tag.Name, tag.CreatedAt, tag.UpdatedAt);
    }
}
