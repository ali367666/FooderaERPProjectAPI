using Application.WarehouseStock.Commands.UpdateDocument;
using FluentValidation;

namespace Application.WarehouseStock.Validators;

public class UpdateWarehouseStockDocumentCommandValidator : AbstractValidator<UpdateWarehouseStockDocumentCommand>
{
    public UpdateWarehouseStockDocumentCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.Request.WarehouseId)
            .GreaterThan(0)
            .WithMessage("Warehouse is required.");

        RuleFor(x => x.Request.Lines)
            .NotEmpty()
            .WithMessage("At least one line is required.");

        RuleForEach(x => x.Request.Lines)
            .ChildRules(line =>
            {
                line.RuleFor(l => l.StockItemId).GreaterThan(0).WithMessage("Stock item is required.");
                line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
                line.RuleFor(l => l.UnitId).GreaterThan(0).WithMessage("Unit is required.");
            });
    }
}
