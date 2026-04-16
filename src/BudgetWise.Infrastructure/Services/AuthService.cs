using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Identity;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace BudgetWise.Infrastructure.Services;

public sealed class AuthService(UserManager<ApplicationUser> userManager) : IAuthService
{
    private const string LoginProvider = "BudgetWise";
    private const string RefreshTokenName = "RefreshToken";
    private const string RefreshTokenExpirationName = "RefreshTokenExpiration";

    public async Task<Result<AuthUserDto>> RegisterAsync(
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return AuthErrors.EmailAlreadyExists;

        var user = new ApplicationUser
        {
            FullName = fullName,
            Email = email,
            UserName = email
        };

        var identityResult = await userManager.CreateAsync(user, password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
            return AuthErrors.RegistrationFailed(errors);
        }

        return new AuthUserDto(user.Id, user.Email!, user.FullName, user.Role);
    }

    public async Task<Result<AuthUserDto>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        if (!user.IsActive)
            return AuthErrors.AccountDisabled;

        if (await userManager.IsLockedOutAsync(user))
            return AuthErrors.AccountLockedOut;

        var passwordValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);
            return AuthErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return new AuthUserDto(user.Id, user.Email!, user.FullName, user.Role);
    }

    public async Task<Result<AuthUserDto>> ValidateRefreshTokenAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return AuthErrors.UserNotFound;

        if (!user.IsActive)
            return AuthErrors.AccountDisabled;

        var storedToken = await userManager.GetAuthenticationTokenAsync(
            user,
            loginProvider: LoginProvider,
            tokenName: RefreshTokenName);

        if (storedToken is null || storedToken != refreshToken)
            return AuthErrors.InvalidRefreshToken;

        var storedExpiration = await userManager.GetAuthenticationTokenAsync(
            user,
            loginProvider: LoginProvider,
            tokenName: RefreshTokenExpirationName);

        if (storedExpiration is null || DateTime.Parse(storedExpiration) < DateTime.UtcNow)
        {
            await RemoveRefreshTokenAsync(user);
            return AuthErrors.InvalidRefreshToken;
        }

        return new AuthUserDto(user.Id, user.Email!, user.FullName, user.Role);
    }

    public async Task StoreRefreshTokenAsync(
        Guid userId,
        string refreshToken,
        int expirationDays,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found when storing refresh token.");

        await userManager.SetAuthenticationTokenAsync(
            user,
            loginProvider: LoginProvider,
            tokenName: RefreshTokenName,
            tokenValue: refreshToken);

        await userManager.SetAuthenticationTokenAsync(
            user,
            loginProvider: LoginProvider,
            tokenName: RefreshTokenExpirationName,
            tokenValue: DateTime.UtcNow.AddDays(expirationDays).ToString("O"));
    }

    private async Task RemoveRefreshTokenAsync(ApplicationUser user)
    {
        await userManager.RemoveAuthenticationTokenAsync(
            user, LoginProvider, RefreshTokenName);
        await userManager.RemoveAuthenticationTokenAsync(
            user, LoginProvider, RefreshTokenExpirationName);
    }
}
