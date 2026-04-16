using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Admin.UseCases;

public sealed class UnlockUserUseCase(IAdminService adminService) : IUseCase
{
    public async Task<Result> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await adminService.UnlockUserAsync(id, cancellationToken);
    }
}
