using BudgetWise.Application.Admin.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Admin.UseCases;

public sealed class UpdateUserUseCase(IAdminService adminService) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return await adminService.UpdateUserAsync(id, request.FullName, request.Email, cancellationToken);
    }
}
