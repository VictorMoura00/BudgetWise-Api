using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.Interfaces;
using BudgetWise.Application.SharedExpenses.DTOs;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.SharedExpenses.UseCases;

public sealed class GetSharedExpenseSummaryUseCase(
    IFamilyGroupRepository familyGroupRepository,
    ISharedExpenseRepository sharedExpenseRepository,
    IUserLookupService userLookupService) : IUseCase
{
    public async Task<Result<SharedExpenseSummaryResponse>> ExecuteAsync(
        Guid familyGroupId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var group = await familyGroupRepository.GetByIdWithMembersAsync(familyGroupId, cancellationToken);

        if (group is null || !group.Members.Any(m => m.UserId == userId))
            return FamilyGroupErrors.NotMember(familyGroupId);

        var expenses = await sharedExpenseRepository.GetAllForGroupAsync(familyGroupId, cancellationToken);

        var allParticipantIds = expenses
            .SelectMany(e => e.Participants)
            .Select(p => p.UserId)
            .Distinct();

        var userNames = await userLookupService.GetDisplayNamesByIdsAsync(allParticipantIds, cancellationToken);

        return BuildSummary(expenses, userNames);
    }

    private static SharedExpenseSummaryResponse BuildSummary(
        IReadOnlyList<SharedExpense> expenses,
        IReadOnlyDictionary<Guid, string> userNames)
    {
        var fullySettled = 0;
        var partiallySettled = 0;
        var unsettled = 0;

        foreach (var expense in expenses)
        {
            var settledCount = expense.Participants.Count(p => p.IsSettled);
            if (settledCount == expense.Participants.Count && expense.Participants.Count > 0)
                fullySettled++;
            else if (settledCount > 0)
                partiallySettled++;
            else
                unsettled++;
        }

        var perParticipant = expenses
            .SelectMany(e => e.Participants)
            .GroupBy(p => p.UserId)
            .Select(g => new ParticipantSummaryResponse(
                UserId: g.Key,
                UserName: userNames.GetValueOrDefault(g.Key, g.Key.ToString()),
                AmountOwed: g.Sum(p => p.AmountOwed),
                AmountSettled: g.Where(p => p.IsSettled).Sum(p => p.AmountOwed),
                AmountPending: g.Where(p => !p.IsSettled).Sum(p => p.AmountOwed)))
            .OrderByDescending(p => p.AmountOwed)
            .ToList()
            .AsReadOnly();

        return new SharedExpenseSummaryResponse(
            TotalExpenses: expenses.Count,
            TotalAmount: expenses.Sum(e => e.TotalAmount),
            TotalSettled: expenses.SelectMany(e => e.Participants).Where(p => p.IsSettled).Sum(p => p.AmountOwed),
            TotalPending: expenses.SelectMany(e => e.Participants).Where(p => !p.IsSettled).Sum(p => p.AmountOwed),
            FullySettledCount: fullySettled,
            PartiallySettledCount: partiallySettled,
            UnsettledCount: unsettled,
            ParticipantTotals: perParticipant);
    }
}
