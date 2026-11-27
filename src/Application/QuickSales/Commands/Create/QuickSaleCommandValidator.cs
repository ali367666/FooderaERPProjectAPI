using FluentValidation;

namespace Application.QuickSales.Commands.Create;

public class QuickSaleCommandValidator : AbstractValidator<QuickSaleCommand>
{
    public QuickSaleCommandValidator()
    {
        RuleFor(x => x.Request.RestaurantId)
            .GreaterThan(0).WithMessage("RestaurantId 0-dan böyük olmalıdır.");

        RuleFor(x => x.Request.WaiterId)
            .GreaterThan(0).WithMessage("WaiterId 0-dan böyük olmalıdır.");

        RuleFor(x => x.Request.Note)
            .MaximumLength(1000).WithMessage("Note 1000 simvoldan çox ola bilməz.");

        RuleFor(x => x.Request.Items)
            .NotEmpty().WithMessage("Satış üçün ən azı bir məhsul seçin.");

        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MenuItemId).GreaterThan(0).WithMessage("MenuItemId 0-dan böyük olmalıdır.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Miqdar 0-dan böyük olmalıdır.");
        });

        RuleFor(x => x.Request.PaymentMethod)
            .NotEmpty().WithMessage("Ödəniş üsulunu seçin.");
    }
}
