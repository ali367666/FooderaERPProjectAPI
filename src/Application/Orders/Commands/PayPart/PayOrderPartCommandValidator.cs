using FluentValidation;

namespace Application.Orders.Commands.PayPart;

public class PayOrderPartCommandValidator : AbstractValidator<PayOrderPartCommand>
{
    public PayOrderPartCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);

        RuleFor(x => x.Request.PaymentMethod)
            .NotEmpty().WithMessage("Ödəniş üsulunu seçin.");

        RuleFor(x => x.Request.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Ödənilən məbləğ mənfi ola bilməz.");

        RuleFor(x => x.Request.Lines)
            .NotEmpty().WithMessage("Ödəniş üçün məhsul seçin.")
            .When(x => x.Request.Amount is null);

        RuleFor(x => x.Request.Amount)
            .GreaterThan(0).WithMessage("Məbləğ 0-dan böyük olmalıdır.")
            .When(x => x.Request.Amount is not null);

        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.OrderLineId).GreaterThan(0);
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Miqdar 0-dan böyük olmalıdır.");
        });
    }
}
