using FluentValidation;

namespace Application.CompanySettings.Commands.Update;

public class UpdateCompanySettingsCommandValidator : AbstractValidator<UpdateCompanySettingsCommand>
{
    public UpdateCompanySettingsCommandValidator()
    {
        RuleFor(x => x.Request.TransparencyLevel)
            .InclusiveBetween(0, 100)
            .When(x => x.Request.TransparencyLevel.HasValue);

        RuleFor(x => x.Request.ReceiptFontSize)
            .GreaterThan(0)
            .When(x => x.Request.ReceiptFontSize.HasValue);

        RuleFor(x => x.Request.CategoryFontSize)
            .GreaterThan(0)
            .When(x => x.Request.CategoryFontSize.HasValue);
    }
}
