using BudgetWise.Application.Admin.DTOs;
using BudgetWise.Application.Identity;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Interfaces;

public interface IAdminService
{
    Task<PaginatedList<AdminUserResponse>> GetUsersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailResponse?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateUserAsync(
        Guid id,
        string fullName,
        string email,
        CancellationToken cancellationToken = default);

    Task<Result> ToggleUserStatusAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> UnlockUserAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> SetUserRoleAsync(
        Guid targetUserId,
        Guid requestingAdminId,
        UserRole role,
        CancellationToken cancellationToken = default);
}
