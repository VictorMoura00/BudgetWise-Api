using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.FamilyGroups.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.FamilyGroups.UseCases;

public sealed class GetFamilyGroupByIdUseCase(
    IFamilyGroupRepository repository) : IUseCase
{
    public async Task<Result<FamilyGroupResponse>> ExecuteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var group = await repository.GetByIdWithMembersAsync(id, cancellationToken);

        if (group is null)
            return FamilyGroupErrors.NotFound(id);

        if (!group.Members.Any(m => m.UserId == userId))
            return FamilyGroupErrors.NotMember(id);

        return MapToResponse(group);
    }

    private static FamilyGroupResponse MapToResponse(FamilyGroup group) =>
        new(group.Id, group.Name, group.Description, group.InviteCode,
            group.CreatedAt, group.UpdatedAt,
            group.Members.Select(m => new FamilyMemberResponse(m.Id, m.UserId, m.Role.ToString(), m.JoinedAt)).ToList());
}
