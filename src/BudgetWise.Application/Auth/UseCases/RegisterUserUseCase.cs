using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Auth.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using Microsoft.Extensions.Configuration;

namespace BudgetWise.Application.Auth.UseCases;

public sealed class RegisterUserUseCase(
    IAuthService authService,
    ITokenService tokenService,
    IConfiguration configuration) : IUseCase
{
    public async Task<Result<AuthResponse>> ExecuteAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var registerResult = await authService.RegisterAsync(
            request.FullName,
            request.Email,
            request.Password,
            cancellationToken);

        if (registerResult.IsFailure)
            return registerResult.Error;

        var user = registerResult.Value;

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email, user.FullName);
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
