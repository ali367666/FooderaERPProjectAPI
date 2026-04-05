using FluentValidation;

namespace Application.WarehouseTransfer.Commands.Update;

public class UpdateWarehouseTransferCommandValidator
    : AbstractValidator<UpdateWarehouseTransferCommand>
{
    public UpdateWarehouseTransferCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Request.FromWarehouseId)
            .GreaterThan(0);

        RuleFor(x => x.Request.ToWarehouseId)
            .GreaterThan(0);

        RuleFor(x => x.Request)
            .Must(x => x.FromWarehouseId != x.ToWarehouseId)
            .WithMessage("From warehouse and To warehouse cannot be the same.");

        RuleFor(x => x.Request.Lines)
            .NotNull()
            .Must(x => x.Count > 0)
            .WithMessage("At least one transfer line is required.");

        RuleForEach(x => x.Request.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.StockItemId)
                    .GreaterThan(0);

                line.RuleFor(l => l.Quantity)
                    .GreaterThan(0);
            });
    }
}