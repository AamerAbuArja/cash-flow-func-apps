using FluentValidation;
using TransactionFuncApp.API.DTOs;

namespace TransactionFuncApp.API.Validators;

public class CompanyValidator : AbstractValidator<CreateCompanyRequest>
{
    public CompanyValidator()
    {
        RuleFor(x => x.name).NotEmpty().WithMessage("Company name is required").MaximumLength(200);
        RuleFor(x => x.baseCurrency).MaximumLength(10).When(x => !string.IsNullOrEmpty(x.baseCurrency));
        RuleFor(x => x.openingBalance).GreaterThanOrEqualTo(0).When(x => x.openingBalance.HasValue);
        RuleFor(x => x.closingBalance).GreaterThanOrEqualTo(0).When(x => x.closingBalance.HasValue);
        RuleFor(x => x.industry).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.industry));
    }
}
