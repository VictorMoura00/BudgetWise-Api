using BudgetWise.Application.Tags.DTOs;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Enums;

namespace BudgetWise.Application.Transactions.DTOs;

public sealed record TransactionResponse(
    Guid Id,
    Guid UserId,
    string Description,
    decimal Amount,
    TransactionType Type,
    DateOnly TransactionDate,
    Guid? CategoryId,
    string? Notes,
    RecurrenceType RecurrenceType,
    DateOnly? RecurrenceEndDate,
    bool IsConfirmed,
    PaymentMethod? PaymentMethod,
    Guid? FamilyGroupId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TagSummary> Tags
);

public sealed record CreateTransactionRequest(
    string Description,
    decimal Amount,
    TransactionType Type,
    DateOnly TransactionDate,
    Guid? CategoryId,
    string? Notes,
    RecurrenceType RecurrenceType,
    DateOnly? RecurrenceEndDate,
    bool IsConfirmed,
    PaymentMethod? PaymentMethod,
    Guid? FamilyGroupId
);

public sealed record UpdateTransactionRequest(
    string Description,
    decimal Amount,
    TransactionType Type,
    DateOnly TransactionDate,
    Guid? CategoryId,
    string? Notes,
    RecurrenceType RecurrenceType,
    DateOnly? RecurrenceEndDate,
    PaymentMethod? PaymentMethod,
    Guid? FamilyGroupId
);

public sealed record GetTransactionsRequest(
    int PageNumber = 1,
    int PageSize = 20,
    TransactionType? Type = null,
    Guid? CategoryId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    bool? IsConfirmed = null
);

public sealed record PaginatedTransactionResponse(
    IReadOnlyCollection<TransactionResponse> Items,
    int PageNumber,
    int PageSize,
    long TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage
)
{
    public static PaginatedTransactionResponse FromPaginatedList(PaginatedList<TransactionResponse> list) => new(
        Items: list.Items,
        PageNumber: list.PageNumber,
        PageSize: list.PageSize,
        TotalCount: list.TotalCount,
        TotalPages: list.TotalPages,
        HasPreviousPage: list.HasPreviousPage,
        HasNextPage: list.HasNextPage
    );
}
