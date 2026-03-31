using FluentValidation;

namespace Application.WarehouseStock.Commands.Update;

public class UpdateWarehouseStockCommandValidator : AbstractValidator<UpdateWarehouseStockCommand>
{
    public UpdateWarehouseStockCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Request.QuantityOnHand)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Request.QuantityOnHand.HasValue)
            .WithMessage("QuantityOnHand cannot be negative.");

        RuleFor(x => x.Request.MinLevel)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Request.MinLevel.HasValue)
            .WithMessage("MinLevel cannot be negative.");
    }
}