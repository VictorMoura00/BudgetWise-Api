using BudgetWise.Application.Interfaces;
using BudgetWise.Application.Tags.Common;
using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.Tags.UseCases;

public sealed class CreateTagUseCase(
    ITagRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result<TagResponse>> ExecuteAsync(
        CreateTagRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsByNameAsync(request.Name, userId, cancellationToken: cancellationToken))
            return TagErrors.DuplicateName(request.Name);

        var tag = Tag.Create(userId, request.Name);
        await repository.AddAsync(tag, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return new TagResponse(tag.Id, tag.Name, tag.CreatedAt, tag.UpdatedAt);
    }
}
