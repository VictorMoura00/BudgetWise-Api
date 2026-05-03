using BudgetWise.Application.FamilyGroups.Common;
using BudgetWise.Application.Interfaces;
using BudgetWise.Application.SharedExpenses.Common;
using BudgetWise.Application.SharedExpenses.DTOs;
using BudgetWise.Domain.Common.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.Interfaces;

namespace BudgetWise.Application.SharedExpenses.UseCases;

public sealed class CreateSharedExpenseUseCase(
    IFamilyGroupRepository familyGroupRepository,
    ITransactionRepository transactionRepository,
    ISharedExpenseRepository sharedExpenseRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : IUseCase
{
    public async Task<Result<SharedExpenseResponse>> ExecuteAsync(
        Guid familyGroupId,
        CreateSharedExpenseRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var group = await familyGroupRepository.GetByIdWithMembersAsync(familyGroupId, cancellationToken);

        if (group is null || !group.Members.Any(m => m.UserId == userId))
            return FamilyGroupErrors.NotMember(familyGroupId);

        if (request.CategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdForUserAsync(
                request.CategoryId.Value, userId, cancellationToken);

            if (category is null)
                return SharedExpenseErrors.CategoryNotFound(request.CategoryId.Value);

            if (category.CategoryType == CategoryType.Income)
                return SharedExpenseErrors.IncompatibleCategory(category.Name);
        }

        var transaction = Transaction.Create(
            userId,
            request.Description,
            request.TotalAmount,
            TransactionType.Expense,
            request.ExpenseDate,
            categoryId: request.CategoryId,
            familyGroupId: familyGroupId);

        var sharedExpense = SharedExpense.Create(
            transaction.Id,
            familyGroupId,
            request.TotalAmount,
            userId,
            request.Description);

        foreach (var p in request.Participants)
            sharedExpense.AddParticipant(p.UserId, p.AmountOwed);

        await transactionRepository.AddAsync(transaction, cancellationToken);
        await sharedExpenseRepository.AddAsync(sharedExpense, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return ToResponse(sharedExpense);
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
