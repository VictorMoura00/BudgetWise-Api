using BudgetWise.Application.Admin.DTOs;
using FluentValidation;

namespace BudgetWise.Application.Admin.Validators;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome completo é obrigatório.")
            .MaximumLength(150).WithMessage("Nome completo deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.");
    }
}

public sealed class SetUserRoleRequestValidator : AbstractValidator<SetUserRoleRequest>
{
    private static readonly string[] ValidRoles = ["User", "Admin"];

    public SetUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role é obrigatório.")
            .Must(r => ValidRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role inválido. Use 'User' ou 'Admin'.");
    }
}
