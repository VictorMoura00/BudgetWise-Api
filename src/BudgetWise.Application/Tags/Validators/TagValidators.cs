using BudgetWise.Application.Tags.DTOs;
using FluentValidation;

namespace BudgetWise.Application.Tags.Validators;

public sealed class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da tag é obrigatório.")
            .MaximumLength(50).WithMessage("O nome da tag deve ter no máximo 50 caracteres.");
    }
}

public sealed class UpdateTagRequestValidator : AbstractValidator<UpdateTagRequest>
{
    public UpdateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da tag é obrigatório.")
            .MaximumLength(50).WithMessage("O nome da tag deve ter no máximo 50 caracteres.");
    }
}
