using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.FamilyGroups.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.FamilyGroups.UseCases;

public sealed class CreateFamilyGroupUseCase(
    IFamilyGroupRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    private const int MaxGroupsPerUser = 5;

    public async Task<Result<FamilyGroupResponse>> ExecuteAsync(
        CreateFamilyGroupRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var count = await repository.CountGroupsForUserAsync(userId, cancellationToken);
        if (count >= MaxGroupsPerUser)
            return FamilyGroupErrors.GroupLimitReached;

        var group = FamilyGroup.Create(userId, request.Name, request.Description);
        await repository.AddAsync(group, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return MapToResponse(group);
    }

    private static FamilyGroupResponse MapToResponse(FamilyGroup group) =>
        new(group.Id, group.Name, group.Description, group.InviteCode,
            group.CreatedAt, group.UpdatedAt,
            group.Members.Select(m => new FamilyMemberResponse(m.Id, m.UserId, string.Empty, string.Empty, m.Role.ToString(), m.JoinedAt)).ToList());
}
