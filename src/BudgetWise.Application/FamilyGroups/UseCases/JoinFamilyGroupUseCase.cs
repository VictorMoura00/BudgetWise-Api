using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.FamilyGroups.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.FamilyGroups.UseCases;

public sealed class JoinFamilyGroupUseCase(
    IFamilyGroupRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    private const int MaxGroupsPerUser = 5;

    public async Task<Result<FamilyGroupSummaryResponse>> ExecuteAsync(
        JoinFamilyGroupRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var count = await repository.CountGroupsForUserAsync(userId, cancellationToken);
        if (count >= MaxGroupsPerUser)
            return FamilyGroupErrors.GroupLimitReached;

        var group = await repository.GetByInviteCodeAsync(request.InviteCode, cancellationToken);
        if (group is null)
            return FamilyGroupErrors.InvalidInviteCode;

        if (group.Members.Any(m => m.UserId == userId))
            return FamilyGroupErrors.AlreadyMember;

        group.AddMember(userId);
        await repository.UpdateAsync(group, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return new FamilyGroupSummaryResponse(
            group.Id, group.Name, group.Description, group.Members.Count, group.CreatedAt);
    }
}
