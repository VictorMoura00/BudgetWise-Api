namespace BudgetWise.Application.SharedExpenses.DTOs;

public sealed record ParticipantRequest(
    Guid UserId,
    decimal AmountOwed
);

public sealed record CreateSharedExpenseRequest(
    string Description,
    decimal TotalAmount,
    DateOnly ExpenseDate,
    Guid? CategoryId,
    IReadOnlyList<ParticipantRequest> Participants
);

public sealed record ParticipantResponse(
    Guid Id,
    Guid UserId,
    decimal AmountOwed,
    bool IsSettled,
    DateTime? SettledAt
);

public sealed record SharedExpenseResponse(
    Guid Id,
    Guid FamilyGroupId,
    Guid TransactionId,
    string? Description,
    decimal TotalAmount,
    Guid CreatedBy,
    decimal TotalSettled,
    decimal TotalPending,
    bool IsFullySettled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ParticipantResponse> Participants
);
