using FluentValidation;
using TransactionFuncApp.API.DTOs;

namespace TransactionFuncApp.Validators;

public class RealizationValidator : AbstractValidator<CreateRealizationRequest>
{
    public RealizationValidator()
    {
        RuleFor(x => x.installmentNum).GreaterThan(0);
        RuleFor(x => x.amount).GreaterThanOrEqualTo(0).When(x => x.amount.HasValue);
        RuleFor(x => x.currency).MaximumLength(10).When(x => !string.IsNullOrEmpty(x.currency));
    }
}
