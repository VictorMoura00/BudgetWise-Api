using BudgetWise.Application.Transactions.DTOs;
using FluentValidation;

namespace BudgetWise.Application.Transactions.Validators;

public sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(255).WithMessage("A descrição deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo de transação inválido.");

        RuleFor(x => x.TransactionDate)
            .NotEmpty().WithMessage("A data da transação é obrigatória.");

        RuleFor(x => x.RecurrenceType)
            .IsInEnum().WithMessage("Tipo de recorrência inválido.");

        When(x => x.PaymentMethod.HasValue, () =>
        {
            RuleFor(x => x.PaymentMethod)
                .IsInEnum().WithMessage("Método de pagamento inválido.");
        });
    }
}

public sealed class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(255).WithMessage("A descrição deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo de transação inválido.");

        RuleFor(x => x.TransactionDate)
            .NotEmpty().WithMessage("A data da transação é obrigatória.");

        RuleFor(x => x.RecurrenceType)
            .IsInEnum().WithMessage("Tipo de recorrência inválido.");

        When(x => x.PaymentMethod.HasValue, () =>
        {
            RuleFor(x => x.PaymentMethod)
                .IsInEnum().WithMessage("Método de pagamento inválido.");
        });
    }
}
