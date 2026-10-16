using Domain.Enums;
using FluentValidation;

namespace Application.Discounts.Commands.SetManual;

public class SetManualDiscountCommandValidator : AbstractValidator<SetManualDiscountCommand>
{
    public SetManualDiscountCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Endirim 0-dan böyük olmalıdır.");

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100).WithMessage("Faiz 100-dən çox ola bilməz.")
            .When(x => x.Type == DiscountType.Percentage);
    }
}
