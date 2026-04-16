using BudgetWise.Application.Admin.Common;
using BudgetWise.Application.Admin.DTOs;
using BudgetWise.Application.Identity;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Admin.UseCases;

public sealed class SetUserRoleUseCase(IAdminService adminService) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid targetUserId,
        Guid requestingAdminId,
        SetUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return Result.Failure(AdminErrors.InvalidRole(request.Role));

        return await adminService.SetUserRoleAsync(targetUserId, requestingAdminId, role, cancellationToken);
    }
}
