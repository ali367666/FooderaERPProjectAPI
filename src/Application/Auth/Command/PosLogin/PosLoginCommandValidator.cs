using FluentValidation;

namespace Application.Auth.Commands.PosLogin;

public sealed class PosLoginCommandValidator : AbstractValidator<PosLoginCommand>
{
    public PosLoginCommandValidator()
    {
        RuleFor(x => x.Request.CompanyId)
            .GreaterThan(0).WithMessage("CompanyId is required");

        RuleFor(x => x.Request)
            .Must(x => !string.IsNullOrWhiteSpace(x.Code) ^ !string.IsNullOrWhiteSpace(x.RfidCardId))
            .WithMessage("Provide either Code or RfidCardId, not both");

        RuleFor(x => x.Request.Code)
            .Matches(@"^\d{4}$|^\d{8}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Code))
            .WithMessage("Code must be 4 or 8 digits");

        RuleFor(x => x.Request.RfidCardId)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.RfidCardId));
    }
}
