using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.FamilyGroups.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.FamilyGroups.UseCases;

public sealed class UpdateFamilyGroupUseCase(
    IFamilyGroupRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result<FamilyGroupResponse>> ExecuteAsync(
        Guid id,
        UpdateFamilyGroupRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var group = await repository.GetByIdWithMembersAsync(id, cancellationToken);

        if (group is null)
            return FamilyGroupErrors.NotFound(id);

        var member = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            return FamilyGroupErrors.NotMember(id);

        if (member.Role != FamilyMemberRole.Owner)
            return FamilyGroupErrors.NotOwner;

        group.Update(request.Name, request.Description);
        await repository.UpdateAsync(group, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return MapToResponse(group);
    }

    private static FamilyGroupResponse MapToResponse(FamilyGroup group) =>
        new(group.Id, group.Name, group.Description, group.InviteCode,
            group.CreatedAt, group.UpdatedAt,
            group.Members.Select(m => new FamilyMemberResponse(m.Id, m.UserId, string.Empty, string.Empty, m.Role.ToString(), m.JoinedAt)).ToList());
}
