using BudgetWise.Application.FamilyGroups.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.FamilyGroups.UseCases;

public sealed class GetFamilyGroupsUseCase(
    IFamilyGroupRepository repository) : IUseCase
{
    public async Task<Result<IReadOnlyList<FamilyGroupSummaryResponse>>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var groups = await repository.GetAllForUserAsync(userId, cancellationToken);

        var response = groups
            .Select(g => new FamilyGroupSummaryResponse(
                g.Id, g.Name, g.Description, g.Members.Count, g.CreatedAt))
            .ToList();

        return response;
    }
}
