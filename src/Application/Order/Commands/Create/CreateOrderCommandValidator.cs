using FluentValidation;

namespace Application.Orders.Commands.Create;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Request.RestaurantId)
            .GreaterThan(0).WithMessage("RestaurantId 0-dan böyük olmalıdır.");

        RuleFor(x => x.Request.TableId)
            .GreaterThan(0).WithMessage("TableId 0-dan böyük olmalıdır.");

        RuleFor(x => x.Request.WaiterId)
            .GreaterThan(0).WithMessage("WaiterId 0-dan böyük olmalıdır.");

        RuleFor(x => x.Request.Note)
            .MaximumLength(1000).WithMessage("Note 1000 simvoldan çox ola bilməz.");
    }
}