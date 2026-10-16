using Domain.Constants;
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

        RuleFor(x => x.Request.ReservationBlockMinutes)
            .Must(m => ReservationRules.BlockMinuteOptions.Contains(m))
            .WithMessage($"Rezerv qadağası üçün seçimlər: {string.Join(", ", ReservationRules.BlockMinuteOptions)} dəqiqə (0 — söndürülüb).");

        RuleFor(x => x.Request.ReservationAutoCancelMinutes)
            .Must(m => ReservationRules.AutoCancelMinuteOptions.Contains(m))
            .WithMessage($"Avtomatik ləğv üçün seçimlər: {string.Join(", ", ReservationRules.AutoCancelMinuteOptions)} dəqiqə (0 — söndürülüb).");

        RuleFor(x => x.Request.CategoryFontSize)
            .GreaterThan(0)
            .When(x => x.Request.CategoryFontSize.HasValue);
    }
}
