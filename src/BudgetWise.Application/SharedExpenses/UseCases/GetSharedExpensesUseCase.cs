using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.Interfaces;
using BudgetWise.Application.SharedExpenses.Common;
using BudgetWise.Application.SharedExpenses.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.SharedExpenses.UseCases;

public sealed class GetSharedExpensesUseCase(
    IFamilyGroupRepository familyGroupRepository,
    ISharedExpenseRepository sharedExpenseRepository) : IUseCase
{
    public async Task<Result<IReadOnlyList<SharedExpenseResponse>>> ExecuteAsync(
        Guid familyGroupId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var group = await familyGroupRepository.GetByIdWithMembersAsync(familyGroupId, cancellationToken);

        if (group is null || !group.Members.Any(m => m.UserId == userId))
            return FamilyGroupErrors.NotMember(familyGroupId);

        var expenses = await sharedExpenseRepository.GetAllForGroupAsync(familyGroupId, cancellationToken);

        return Result<IReadOnlyList<SharedExpenseResponse>>.Success(
            expenses.Select(ToResponse).ToList().AsReadOnly());
    }

    private static SharedExpenseResponse ToResponse(SharedExpense se)
    {
        var totalSettled = se.Participants.Where(p => p.IsSettled).Sum(p => p.AmountOwed);
        var totalPending = se.Participants.Where(p => !p.IsSettled).Sum(p => p.AmountOwed);

        return new SharedExpenseResponse(
            se.Id,
            se.FamilyGroupId,
            se.TransactionId,
            se.Description,
            se.TotalAmount,
            se.CreatedBy,
            totalSettled,
            totalPending,
            se.Participants.Count > 0 && se.Participants.All(p => p.IsSettled),
            se.CreatedAt,
            se.UpdatedAt,
            se.Participants
                .Select(p => new ParticipantResponse(p.Id, p.UserId, p.AmountOwed, p.IsSettled, p.SettledAt))
                .ToList()
                .AsReadOnly());
    }
}
