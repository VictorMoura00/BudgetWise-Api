using BudgetWise.Domain.Common.Results;

namespace BudgetWise.Application.Auth.Common;

/// <summary>
/// Erros de domínio centralizados para o módulo de autenticação.
/// </summary>
public static class AuthErrors
{
    public static Error InvalidCredentials =>
        Error.Unauthorized("Auth.InvalidCredentials", "Email ou senha inválidos.");

    public static Error EmailAlreadyExists =>
        Error.Conflict("Auth.EmailAlreadyExists", "Este email já está cadastrado.");

    public static Error AccountDisabled =>
        Error.Unauthorized("Auth.AccountDisabled", "Esta conta está desativada.");

    public static Error AccountLockedOut =>
        Error.Unauthorized("Auth.AccountLockedOut", "Conta bloqueada por excesso de tentativas. Tente novamente mais tarde.");

    public static Error InvalidRefreshToken =>
        Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token inválido ou expirado.");

    public static Error UserNotFound =>
        Error.NotFound("Auth.UserNotFound", "Usuário não encontrado.");

    public static Error RegistrationFailed(string details) =>
        Error.Validation("Auth.RegistrationFailed", details);
}