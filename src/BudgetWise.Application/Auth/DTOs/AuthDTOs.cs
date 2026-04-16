namespace BudgetWise.Application.Auth.DTOs;

public sealed record RegisterUserRequest(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword);

public sealed record LoginUserRequest(
    string Email,
    string Password);

public sealed record RefreshTokenRequest(
    Guid UserId,
    string RefreshToken);

/// <summary>
/// Resposta padrão para operações de autenticação (Register, Login, RefreshToken).
/// </summary>
public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string AccessToken,
    string RefreshToken);
