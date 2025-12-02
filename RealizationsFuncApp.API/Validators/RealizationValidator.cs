using FluentValidation;

public class RealizationValidator : AbstractValidator<RealizationDto>
{
    public RealizationValidator()
    {
        RuleFor(x => x.tenantId).NotEmpty().WithMessage("tenantId is required");
        RuleFor(x => x.companyId).NotEmpty().WithMessage("companyId is required");
        RuleFor(x => x.transactionId).NotEmpty().WithMessage("transactionId is required");
        RuleFor(x => x.id).NotEmpty().WithMessage("id is required");
        RuleFor(x => x.installmentNum).GreaterThanOrEqualTo(0);

        RuleFor(x => x.amount).GreaterThanOrEqualTo(0).When(x => x.amount.HasValue);
        RuleFor(x => x.amountInBase).GreaterThanOrEqualTo(0).When(x => x.amountInBase.HasValue);

        RuleFor(x => x.flow)
            .NotEmpty()
            .Must(v => v == "in" || v == "out" || v == "internal")
            .WithMessage("flow must be one of: in, out, internal.");

        RuleFor(x => x.currency).Length(3).When(x => !string.IsNullOrEmpty(x.currency));
    }
}
