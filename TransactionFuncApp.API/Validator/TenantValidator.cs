using FluentValidation;
using TransactionFuncApp.API.DTOs;

namespace TransactionFuncApp.API.Validators;

public class TenantValidator : AbstractValidator<CreateTenantRequest>
{
    public TenantValidator()
    {
        RuleFor(x => x.name).NotEmpty().WithMessage("Tenant name is required").MaximumLength(200);
        RuleFor(x => x.baseCurrency).MaximumLength(10).When(x => !string.IsNullOrEmpty(x.baseCurrency));
        RuleFor(x => x.subscription).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.subscription));
    }
}
