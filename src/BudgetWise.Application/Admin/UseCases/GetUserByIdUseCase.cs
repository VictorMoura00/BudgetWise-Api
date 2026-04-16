using BudgetWise.Application.Admin.Common;
using BudgetWise.Application.Admin.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Admin.UseCases;

public sealed class GetUserByIdUseCase(IAdminService adminService) : IUseCase
{
    public async Task<Result<AdminUserDetailResponse>> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await adminService.GetUserByIdAsync(id, cancellationToken);

        if (user is null)
            return AdminErrors.UserNotFound(id);

        return user;
    }
}
