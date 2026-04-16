using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using Microsoft.Extensions.Configuration;

namespace BudgetWise.Application.Auth.UseCases;

public sealed class RefreshTokenUseCase(
    IAuthService authService,
    ITokenService tokenService,
    IConfiguration configuration) : IUseCase
{
    public async Task<Result<AuthResponse>> ExecuteAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await authService.ValidateRefreshTokenAsync(
            request.UserId,
            request.RefreshToken,
            cancellationToken);

        if (validationResult.IsFailure)
            return validationResult.Error;

        var user = validationResult.Value;

        var newAccessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.FullName, user.Role);
        var newRefreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenExpDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        await authService.StoreRefreshTokenAsync(
            user.Id,
            newRefreshToken,
            refreshTokenExpDays,
            cancellationToken);

        return new AuthResponse(
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken);
    }
}
