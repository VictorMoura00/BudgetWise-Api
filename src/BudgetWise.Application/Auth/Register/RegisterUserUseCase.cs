using BudgetWise.Application.Auth.Common;
using BudgetWise.Application.Identity;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Common.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace BudgetWise.Application.Auth.Register;

public sealed class RegisterUserUseCase(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IConfiguration configuration)
{
    public async Task<Result<AuthResponse>> ExecuteAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return AuthErrors.EmailAlreadyExists;

        var user = new ApplicationUser
        {
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email // Identity exige UserName; usamos o email
        };

        var identityResult = await userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
            return AuthErrors.RegistrationFailed(errors);
        }

        var accessToken = tokenService.GenerateAccessToken(user.Id, user.Email!, user.FullName);
        var refreshToken = tokenService.GenerateRefreshToken();

        var refreshTokenExpDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        await userManager.SetAuthenticationTokenAsync(
            user,
            loginProvider: "BudgetWise",
            name: "RefreshToken",
            value: refreshToken);

        // Armazena a data de expiração do refresh token como token separado
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