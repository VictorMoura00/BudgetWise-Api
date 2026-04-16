using BudgetWise.Application.Admin.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Pagination;
using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Admin.UseCases;

public sealed class GetUsersUseCase(IAdminService adminService) : IUseCase
{
    public async Task<Result<PaginatedList<AdminUserResponse>>> ExecuteAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await adminService.GetUsersAsync(page, pageSize, cancellationToken);
        return result;
    }
}
