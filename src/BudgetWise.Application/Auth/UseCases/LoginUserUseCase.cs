using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using Microsoft.Extensions.Configuration;

namespace BudgetWise.Application.Auth.UseCases;

public sealed class LoginUserUseCase(
    IAuthService authService,
    ITokenService tokenService,
    IConfiguration configuration) : IUseCase
{
    public async Task<Result<AuthResponse>> ExecuteAsync(
        LoginUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var credentialsResult = await authService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (credentialsResult.IsFailure)
            return credentialsResult.Error;

        var user = credentialsResult.Value;

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.FullName, user.Role);
        var refreshToken = tokenService.GenerateRefreshToken();
        var expDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        await authService.StoreRefreshTokenAsync(user.Id, refreshToken, expDays, cancellationToken);

        return new AuthResponse(
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            AccessToken: accessToken,
            RefreshToken: refreshToken);
    }
}
