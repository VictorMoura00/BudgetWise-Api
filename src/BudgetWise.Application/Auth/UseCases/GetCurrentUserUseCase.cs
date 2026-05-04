using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Auth.UseCases;

public sealed class GetCurrentUserUseCase(IAuthService authService) : IUseCase
{
    public async Task<Result<CurrentUserResponse>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await authService.GetByIdAsync(userId, cancellationToken);
        if (result.IsFailure)
            return result.Error;

        var user = result.Value;
        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.IsActive);
    }
}
