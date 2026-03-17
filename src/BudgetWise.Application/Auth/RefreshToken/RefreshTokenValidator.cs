using FluentValidation;

namespace BudgetWise.Application.Auth.RefreshToken;

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