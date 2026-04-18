using FluentValidation;
using TransactionFuncApp.API.DTOs;
using TransactionFuncApp.API.Enums.TransactionEnums;

namespace TransactionFuncApp.API.Validators;

public class ManualInstallmentValidator : AbstractValidator<ManualInstallmentDto>
{
    public ManualInstallmentValidator()
    {
        RuleFor(x => x.installmentNum)
            .GreaterThan(0).WithMessage("installmentNum must be greater than 0.");

        RuleFor(x => x.amount)
            .GreaterThan(0).WithMessage("Each manual installment amount must be greater than 0.");

        RuleFor(x => x.expectedDate)
            .NotEmpty().WithMessage("Each manual installment requires an expectedDate.")
            .Must(d => d.HasValue && DateTime.TryParse(d.Value.ToString("yyyy-MM-dd"), out _))
            .WithMessage("expectedDate must be a valid ISO date string.");
    }
}

public class PercentageInstallmentValidator : AbstractValidator<PercentageInstallmentDto>
{
    public PercentageInstallmentValidator()
    {
        RuleFor(x => x.installmentNum)
            .GreaterThan(0).WithMessage("installmentNum must be greater than 0.");

        RuleFor(x => x.percentage)
            .GreaterThan(0).WithMessage("Each percentage installment must be greater than 0%.")
            .LessThanOrEqualTo(100).WithMessage("Each percentage installment cannot exceed 100%.");

        RuleFor(x => x.expectedDate)
            .NotEmpty().WithMessage("Each percentage installment requires an expectedDate.")
            .Must(d => d.HasValue && DateTime.TryParse(d.Value.ToString("yyyy-MM-dd"), out _))
            .WithMessage("expectedDate must be a valid ISO date string.");
    }
}

public class TransactionValidator : AbstractValidator<CreateTransactionRequest>
{
    public TransactionValidator()
    {
        RuleFor(x => x.type)
            .IsInEnum()
            .WithMessage("Invalid transaction type.");

        RuleFor(x => x.id)
            .NotEmpty().WithMessage("Transaction ID is required.")
            .MaximumLength(100);

        RuleFor(x => x.category)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.amount)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.currency)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.installmentMode)
            .IsInEnum()
            .WithMessage("Invalid installment mode.");

        When(x => !string.IsNullOrEmpty(x.dueDate), () =>
        {
            RuleFor(x => x.dueDate)
                .Must(d => DateTime.TryParse(d, out _))
                .WithMessage("dueDate must be a valid ISO date string.");
        });

        // --- Auto ---
        When(x => x.installmentMode == InstallmentMode.Auto, () =>
        {
            RuleFor(x => x.installmentCount)
                .NotNull().WithMessage("Auto mode requires installmentCount.")
                .GreaterThan(0).WithMessage("installmentCount must be greater than 0.");

            RuleFor(x => x.installmentInterval)
                .NotNull().WithMessage("Auto mode requires installmentInterval.")
                .GreaterThan(0).WithMessage("installmentInterval must be greater than 0.");
        });

        // --- Manual ---
        When(x => x.installmentMode == InstallmentMode.Manual, () =>
        {
            RuleFor(x => x.manualInstallments)
                .NotNull().WithMessage("Manual mode requires manualInstallments.")
                .Must(list => list != null && list.Count > 0)
                .WithMessage("manualInstallments must contain at least one entry.");

            When(x => x.manualInstallments != null, () =>
            {
                RuleForEach(x => x.manualInstallments).SetValidator(new ManualInstallmentValidator());

                RuleFor(x => x.manualInstallments)
                    .Must((req, list) => list!.Sum(i => i.amount) == req.amount)
                    .WithMessage("Manual installment amounts must sum to the transaction amount.");
            });
        });

        // --- Percentage ---
        When(x => x.installmentMode == InstallmentMode.Percentage, () =>
        {
            RuleFor(x => x.percentageInstallments)
                .NotNull().WithMessage("Percentage mode requires percentageInstallments.")
                .Must(list => list != null && list.Count > 0)
                .WithMessage("percentageInstallments must contain at least one entry.");

            When(x => x.percentageInstallments != null, () =>
            {
                RuleForEach(x => x.percentageInstallments).SetValidator(new PercentageInstallmentValidator());

                RuleFor(x => x.percentageInstallments)
                    .Must(list => Math.Abs(list!.Sum(i => i.percentage) - 100m) <= 0.01m)
                    .WithMessage("Percentage installments must sum to 100%.");
            });
        });

        // --- Recurring ---
        When(x => x.installmentMode == InstallmentMode.Recurring, () =>
        {
            RuleFor(x => x.recurringFrequency)
                .NotEmpty().WithMessage("Recurring mode requires recurringFrequency.")
                .IsEnumName(typeof(RecurringFrequency), caseSensitive: false)
                .WithMessage("recurringFrequency must be one of: Weekly, Monthly, Quarterly, Yearly.");

            When(x => !string.IsNullOrEmpty(x.recurringEndDate), () =>
            {
                RuleFor(x => x.recurringEndDate)
                    .Must(d => DateTime.TryParse(d, out _))
                    .WithMessage("recurringEndDate must be a valid ISO date string.");
            });

            When(x => x.recurringEndAfter.HasValue, () =>
            {
                RuleFor(x => x.recurringEndAfter)
                    .GreaterThan(0)
                    .WithMessage("recurringEndAfter must be greater than 0.");
            });

            RuleFor(x => x)
                .Must(x => !(x.recurringEndAfter.HasValue && !string.IsNullOrEmpty(x.recurringEndDate)))
                .WithMessage("Specify either recurringEndAfter or recurringEndDate, not both.");
        });
    }
}