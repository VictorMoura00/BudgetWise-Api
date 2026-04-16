using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.FamilyGroups.UseCases;

public sealed class LeaveFamilyGroupUseCase(
    IFamilyGroupRepository repository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var group = await repository.GetByIdWithMembersAsync(id, cancellationToken);

        if (group is null)
            return Result.Failure(FamilyGroupErrors.NotFound(id));

        var member = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null)
            return Result.Failure(FamilyGroupErrors.NotMember(id));

        if (member.Role == FamilyMemberRole.Owner)
            return Result.Failure(FamilyGroupErrors.OwnerCannotLeave);

        group.RemoveMember(userId, userId);
        await repository.UpdateAsync(group, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
