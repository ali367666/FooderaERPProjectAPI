using FluentValidation;

namespace Application.OrderLines.Commands.Update;

public class UpdateOrderLineCommandValidator : AbstractValidator<UpdateOrderLineCommand>
{
    public UpdateOrderLineCommandValidator()
    {
        RuleFor(x => x.Request.Id)
            .GreaterThan(0);

        RuleFor(x => x.Request.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.Request.Note)
            .MaximumLength(500);

        RuleFor(x => x.Request.Status)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Status));
    }
}