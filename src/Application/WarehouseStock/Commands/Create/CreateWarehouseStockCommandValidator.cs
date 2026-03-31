using Application.WarehouseStock.Commands.Create;
using FluentValidation;

namespace Application.WarehouseStock.Validators;

public class CreateWarehouseStockCommandValidator : AbstractValidator<CreateWarehouseStockCommand>
{
    public CreateWarehouseStockCommandValidator()
    {
        RuleFor(x => x.Request.StockItemId)
            .GreaterThan(0)
            .WithMessage("StockItemId must be greater than 0.");

        RuleFor(x => x.Request.WarehouseId)
            .GreaterThan(0)
            .WithMessage("WarehouseId must be greater than 0.");

        RuleFor(x => x.Request.QuantityOnHand)
            .GreaterThanOrEqualTo(0)
            .WithMessage("QuantityOnHand cannot be negative.");

        RuleFor(x => x.Request.MinLevel)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Request.MinLevel.HasValue)
            .WithMessage("MinLevel cannot be negative.");
    }
}