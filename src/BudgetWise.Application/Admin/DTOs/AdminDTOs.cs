namespace BudgetWise.Application.Admin.DTOs;

public sealed record AdminUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    bool IsLockedOut,
    DateTime CreatedAt);

public sealed record AdminUserDetailResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    bool IsLockedOut,
    int AccessFailedCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record UpdateUserRequest(string FullName, string Email);

public sealed record SetUserRoleRequest(string Role);
