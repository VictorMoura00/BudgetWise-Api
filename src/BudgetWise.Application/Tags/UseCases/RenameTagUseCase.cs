using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.Common;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Tags.UseCases;

public sealed class RenameTagUseCase(
    ITagRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result<TagResponse>> ExecuteAsync(
        Guid id,
        UpdateTagRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tag = await repository.GetByIdForUserAsync(id, userId, cancellationToken);

        if (tag is null)
            return TagErrors.NotFound(id);

        if (await repository.ExistsByNameAsync(request.Name, userId, id, cancellationToken))
            return TagErrors.DuplicateName(request.Name);

        tag.Rename(request.Name);
        await repository.UpdateAsync(tag, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return new TagResponse(tag.Id, tag.Name, tag.CreatedAt, tag.UpdatedAt);
    }
}
