using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.FamilyGroups.UseCases;

public sealed class RemoveMemberUseCase(
    IFamilyGroupRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        Guid memberUserId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await repository.GetByIdWithMembersAsync(id, cancellationToken);

        if (group is null)
            return Result.Failure(FamilyGroupErrors.NotFound(id));

        var requester = group.Members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requester is null)
            return Result.Failure(FamilyGroupErrors.NotMember(id));

        if (requester.Role != FamilyMemberRole.Owner)
            return Result.Failure(FamilyGroupErrors.NotOwner);

        if (!group.Members.Any(m => m.UserId == memberUserId))
            return Result.Failure(FamilyGroupErrors.MemberNotFound(memberUserId));

        group.RemoveMember(memberUserId, requestingUserId);
        await repository.UpdateAsync(group, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
