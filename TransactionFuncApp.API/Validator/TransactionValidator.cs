using FluentValidation;
using TransactionFuncApp.API.DTOs;

namespace TransactionFuncApp.API.Validators;

public class TransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public TransactionValidator()
    {
        RuleFor(x => x.type).NotEmpty().MaximumLength(100);
        RuleFor(x => x.category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.description).NotEmpty().MaximumLength(2000);

        RuleFor(x => x.amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.currency).NotEmpty().MaximumLength(10);

        RuleFor(x => x.installmentMode).NotNull().MaximumLength(50);
        When(x => x.installmentMode != null && x.installmentMode.ToLower() != "none", () =>
        {
            RuleFor(x => x.installmentCount).NotNull().GreaterThan(0);
            RuleFor(x => x.installmentInterval).NotNull().GreaterThan(0);
        });

        When(x => !string.IsNullOrEmpty(x.dueDate), () =>
        {
            RuleFor(x => x.dueDate).Must(d =>
            {
                return DateTime.TryParse(d, out _);
            }).WithMessage("dueDate must be a valid date string (ISO).");
        });
    }
}
