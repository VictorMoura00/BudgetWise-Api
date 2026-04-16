using BudgetWise.Application.Admin.Common;
using BudgetWise.Application.Admin.DTOs;
using BudgetWise.Application.Identity;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Common.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BudgetWise.Infrastructure.Services;

public sealed class AdminService(UserManager<ApplicationUser> userManager) : IAdminService
{
    public async Task<PaginatedList<AdminUserResponse>> GetUsersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = userManager.Users.OrderBy(u => u.FullName);

        var total = await query.CountAsync(cancellationToken);

        var users = await query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = users.Select(u => new AdminUserResponse(
            u.Id,
            u.FullName,
            u.Email!,
            u.Role.ToString(),
            u.IsActive,
            u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
            u.CreatedAt)).ToList();

        return new PaginatedList<AdminUserResponse>(items, total, page, pageSize);
    }

    public async Task<AdminUserDetailResponse?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null) return null;

        return new AdminUserDetailResponse(
            user.Id,
            user.FullName,
            user.Email!,
            user.Role.ToString(),
            user.IsActive,
            user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            user.AccessFailedCount,
            user.CreatedAt,
            user.UpdatedAt);
    }

    public async Task<Result> UpdateUserAsync(
        Guid id,
        string fullName,
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result.Failure(AdminErrors.UserNotFound(id));

        var existingWithEmail = await userManager.FindByEmailAsync(email);
        if (existingWithEmail is not null && existingWithEmail.Id != id)
            return Result.Failure(AdminErrors.EmailAlreadyInUse(email));

        user.FullName = fullName;
        user.Email = email;
        user.UserName = email;
        user.UpdatedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);
        return Result.Success();
    }

    public async Task<Result> ToggleUserStatusAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result.Failure(AdminErrors.UserNotFound(id));

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);
        return Result.Success();
    }

    public async Task<Result> UnlockUserAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result.Failure(AdminErrors.UserNotFound(id));

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        return Result.Success();
    }

    public async Task<Result> SetUserRoleAsync(
        Guid targetUserId,
        Guid requestingAdminId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (targetUserId == requestingAdminId && role != UserRole.Admin)
            return Result.Failure(AdminErrors.CannotSelfDemote);

        var user = await userManager.FindByIdAsync(targetUserId.ToString());
        if (user is null)
            return Result.Failure(AdminErrors.UserNotFound(targetUserId));

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);
        return Result.Success();
    }
}
