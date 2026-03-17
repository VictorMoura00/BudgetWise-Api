namespace BudgetWise.Application.Auth.Common;

/// <summary>
/// Resposta padrão para operações de autenticação (Register, Login, RefreshToken).
/// </summary>
public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string AccessToken,
    string RefreshToken
);