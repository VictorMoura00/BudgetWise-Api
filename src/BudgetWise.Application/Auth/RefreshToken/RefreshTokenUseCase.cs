using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Identity;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace BudgetWise.Application.Auth.RefreshToken;

public sealed class RefreshTokenUseCase(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IConfiguration configuration)
{
    public async Task<Result<AuthResponse>> ExecuteAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
            return AuthErrors.UserNotFound;

        if (!user.IsActive)
            return AuthErrors.AccountDisabled;

        // Busca o refresh token armazenado
        var storedToken = await userManager.GetAuthenticationTokenAsync(
            user,
            loginProvider: "BudgetWise",
            name: "RefreshToken");

        if (storedToken is null || storedToken != request.RefreshToken)
            return AuthErrors.InvalidRefreshToken;

        // Verifica expiração
        var storedExpiration = await userManager.GetAuthenticationTokenAsync(
            user,
            loginProvider: "BudgetWise",
            name: "RefreshTokenExpiration");

        if (storedExpiration is null || DateTime.Parse(storedExpiration) < DateTime.UtcNow)
        {
            // Remove tokens expirados
            await userManager.RemoveAuthenticationTokenAsync(
                user, "BudgetWise", "RefreshToken");
            await userManager.RemoveAuthenticationTokenAsync(
                user, "BudgetWise", "RefreshTokenExpiration");

            return AuthErrors.InvalidRefreshToken;
        }

        // Rotação obrigatória: gera novo par de tokens
        var newAccessToken = tokenService.GenerateAccessToken(user.Id, user.Email!, user.FullName);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        var refreshTokenExpDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        await userManager.SetAuthenticationTokenAsync(
            user,
            loginProvider: "BudgetWise",
            name: "RefreshToken",
            value: newRefreshToken);

        await userManager.SetAuthenticationTokenAsync(
            user,
            loginProvider: "BudgetWise",
            name: "RefreshTokenExpiration",
            value: DateTime.UtcNow.AddDays(refreshTokenExpDays).ToString("O"));

        return new AuthResponse(
            UserId: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken);
    }
}