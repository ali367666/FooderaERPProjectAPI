using Domain.Constants;
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

        // A store staff card scanned into the code field is a long run of digits, so the length is not capped at the code's own
        // limit; the resolver matches the staff code first and the card barcode after it.
        RuleFor(x => x.Request.Code)
            .Matches(@"^[A-Za-z0-9]+$")
            .MaximumLength(450)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Code))
            .WithMessage("Code must be letters or digits");

        RuleFor(x => x.Request.RfidCardId)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.RfidCardId));
    }
}
