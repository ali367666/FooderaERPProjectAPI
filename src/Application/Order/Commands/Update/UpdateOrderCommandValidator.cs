using FluentValidation;

namespace Application.Orders.Commands.Update;

public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.Request.Id)
            .GreaterThan(0);

        RuleFor(x => x.Request.RestaurantId)
            .GreaterThan(0);

        RuleFor(x => x.Request.TableId)
            .GreaterThan(0);

        RuleFor(x => x.Request.WaiterId)
            .GreaterThan(0);

        RuleFor(x => x.Request.Note)
            .MaximumLength(1000);

        RuleFor(x => x.Request.Status)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Status));
    }
}