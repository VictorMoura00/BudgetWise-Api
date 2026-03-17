using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using BudgetWise.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace BudgetWise.Application.Auth.Login;

public sealed class LoginUserUseCase(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IConfiguration configuration)
{
    public async Task<Result<AuthResponse>> ExecuteAsync(
        LoginUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        if (!user.IsActive)
            return AuthErrors.AccountDisabled;

        if (await userManager.IsLockedOutAsync(user))
            return AuthErrors.AccountLockedOut;

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);
            return AuthErrors.InvalidCredentials;
        }

        // Reset do contador de tentativas após login bem-sucedido
        await userManager.ResetAccessFailedCountAsync(user);

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email!, user.FullName);
        var refreshToken = tokenService.GenerateRefreshToken();

        var refreshTokenExpDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        // Rotação obrigatória: substitui o refresh token anterior
        await userManager.SetAuthenticationTokenAsync(
            user,
            loginProvider: "BudgetWise",
            name: "RefreshToken",
            value: refreshToken);

        await userManager.SetAuthenticationTokenAsync(
            user,
            loginProvider: "BudgetWise",
            name: "RefreshTokenExpiration",
            value: DateTime.UtcNow.AddDays(refreshTokenExpDays).ToString("O"));

        return new AuthResponse(
            UserId: user.Id,
            Email: user.Email!,
            FullName: user.FullName,
            AccessToken: accessToken,
            RefreshToken: refreshToken);
    }
}