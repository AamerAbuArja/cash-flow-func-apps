using FluentValidation;
using System;

public class TransactionDtoValidator : AbstractValidator<TransactionDto>
{
    public TransactionDtoValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();

        RuleFor(x => x.Amount).GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.InstallmentMode)
            .NotEmpty()
            .Must(mode => mode == "none" || mode == "periodic")
            .WithMessage("installmentMode must be 'none' or 'periodic'.");

        When(x => x.InstallmentMode == "periodic", () =>
        {
            RuleFor(x => x.InstallmentCount)
                .NotNull()
                .GreaterThan(0);

            RuleFor(x => x.InstallmentInterval)
                .NotNull()
                .GreaterThan(0);

            RuleFor(x => x.Installments)
                .NotNull()
                .Must(i => i.Count > 0).WithMessage("installments must have at least one item");

            RuleForEach(x => x.Installments!)
                .ChildRules(installment =>
                {
                    installment.RuleFor(i => i.Number).GreaterThan(0);
                    installment.RuleFor(i => i.Amount).GreaterThan(0);
                    installment.RuleFor(i => i.DueDate).GreaterThan(DateTime.UtcNow.AddDays(-1));
                });
        });
    }
}
