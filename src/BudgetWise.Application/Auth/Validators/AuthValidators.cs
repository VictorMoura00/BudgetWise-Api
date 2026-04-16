using BudgetWise.Application.Auth.DTOs;
using FluentValidation;

namespace BudgetWise.Application.Auth.Validators;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(150).WithMessage("Nome completo deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres.")
            .Matches("[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("Senha deve conter pelo menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Senha deve conter pelo menos um caractere especial.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Confirmação de senha não confere.");
    }
}

public sealed class LoginUserValidator : AbstractValidator<LoginUserRequest>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.");
    }
}

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId é obrigatório.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token é obrigatório.");
    }
}
