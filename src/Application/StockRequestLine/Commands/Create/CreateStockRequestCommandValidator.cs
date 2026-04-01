using FluentValidation;
using Application.StockRequests.Dtos.Request;

namespace Application.StockRequests.Commands.Create;

public class CreateStockRequestCommandValidator : AbstractValidator<CreateStockRequestCommand>
{
    public CreateStockRequestCommandValidator()
    {
        RuleFor(x => x.Request.CompanyId).GreaterThan(0);

        RuleFor(x => x.Request.RequestingWarehouseId).GreaterThan(0);
        RuleFor(x => x.Request.SupplyingWarehouseId).GreaterThan(0);

        RuleFor(x => x.Request.RequestingWarehouseId)
            .NotEqual(x => x.Request.SupplyingWarehouseId)
            .WithMessage("Warehouses cannot be the same.");

        RuleFor(x => x.Request.Lines)
            .NotEmpty()
            .WithMessage("At least one line is required.");

        RuleForEach(x => x.Request.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(x => x.StockItemId).GreaterThan(0);
                line.RuleFor(x => x.Quantity).GreaterThan(0);
            });
    }
}