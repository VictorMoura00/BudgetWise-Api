using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.Interfaces;
using BudgetWise.Application.SharedExpenses.Common;
using BudgetWise.Application.SharedExpenses.DTOs;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.SharedExpenses.UseCases;

public sealed class SettleSharedExpenseParticipantUseCase(
    IFamilyGroupRepository familyGroupRepository,
    ISharedExpenseRepository sharedExpenseRepository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result<SharedExpenseResponse>> ExecuteAsync(
        Guid familyGroupId,
        Guid sharedExpenseId,
        Guid participantUserId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var group = await familyGroupRepository.GetByIdWithMembersAsync(familyGroupId, cancellationToken);

        if (group is null || !group.Members.Any(m => m.UserId == userId))
            return FamilyGroupErrors.NotMember(familyGroupId);

        var expense = await sharedExpenseRepository.GetByIdForGroupAsync(
            sharedExpenseId, familyGroupId, cancellationToken);

        if (expense is null)
            return SharedExpenseErrors.NotFound(sharedExpenseId);

        var participant = expense.Participants.FirstOrDefault(p => p.UserId == participantUserId);

        if (participant is null)
            return SharedExpenseErrors.ParticipantNotFound(participantUserId);

        if (participant.IsSettled)
            return SharedExpenseErrors.AlreadySettled;

        participant.Settle();
        expense.CheckAndRaiseFullySettled();

        await unitOfWork.CommitAsync(cancellationToken);

        return ToResponse(expense);
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
