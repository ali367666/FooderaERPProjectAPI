using FluentValidation;

namespace Application.Orders.Commands.SetServiceCharge;

public class SetOrderServiceChargeCommandValidator : AbstractValidator<SetOrderServiceChargeCommand>
{
    public SetOrderServiceChargeCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Servis haqqı mənfi ola bilməz.");
    }
}
