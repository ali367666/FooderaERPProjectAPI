using FluentValidation;

namespace Application.StockRequests.Commands.Update;

public class UpdateStockRequestCommandValidator : AbstractValidator<UpdateStockRequestCommand>
{
    public UpdateStockRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Request.RequestingWarehouseId)
            .GreaterThan(0);

        RuleFor(x => x.Request.SupplyingWarehouseId)
            .GreaterThan(0);

        RuleFor(x => x.Request)
            .Must(x => x.RequestingWarehouseId != x.SupplyingWarehouseId)
            .WithMessage("Requesting warehouse and supplying warehouse cannot be the same.");

        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.StockItemId)
                .GreaterThan(0);

            line.RuleFor(x => x.Quantity)
                .GreaterThan(0);
        });
    }
}