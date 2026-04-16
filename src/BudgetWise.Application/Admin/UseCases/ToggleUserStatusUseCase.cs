using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Admin.UseCases;

public sealed class ToggleUserStatusUseCase(IAdminService adminService) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await adminService.ToggleUserStatusAsync(id, cancellationToken);
    }
}
