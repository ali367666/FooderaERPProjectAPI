using FluentValidation;

namespace Application.OrderLines.Commands.Add;

public class AddOrderLineCommandValidator : AbstractValidator<AddOrderLineCommand>
{
    public AddOrderLineCommandValidator()
    {
        RuleFor(x => x.Request.OrderId)
            .GreaterThan(0);

        RuleFor(x => x.Request.MenuItemId)
            .GreaterThan(0);

        RuleFor(x => x.Request.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.Request.Note)
            .MaximumLength(500);
    }
}