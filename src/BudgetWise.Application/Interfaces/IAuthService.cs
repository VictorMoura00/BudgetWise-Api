using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Interfaces;

/// <summary>
/// Abstrai as operações de autenticação que dependem do Identity.
/// Implementação concreta fica na Infrastructure.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Cria um novo usuário com email e senha.
    /// </summary>
    Task<Result<AuthUserDto>> RegisterAsync(
        string fullName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida credenciais e retorna os dados do usuário se bem-sucedido.
    /// Gerencia lockout e contador de tentativas.
    /// </summary>
    Task<Result<AuthUserDto>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida o refresh token armazenado e retorna os dados do usuário.
    /// </summary>
    Task<Result<AuthUserDto>> ValidateRefreshTokenAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Armazena um novo refresh token para o usuário (rotação obrigatória).
    /// </summary>
    Task StoreRefreshTokenAsync(
        Guid userId,
        string refreshToken,
        int expirationDays,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO que transporta dados do usuário autenticado entre a Infrastructure e a Application.
/// Evita que a Application conheça o ApplicationUser.
/// </summary>
public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string FullName
);