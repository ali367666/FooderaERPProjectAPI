using Application.StockItem.Commands.Update;
using FluentValidation;

namespace Application.StockItem.Commands.Patch;

public class PatchStockItemCommandValidator : AbstractValidator<PatchStockItemCommand>
{
    public PatchStockItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0.");

        When(x => x.Request.Name is not null, () =>
        {
            RuleFor(x => x.Request.Name)
                .NotEmpty().WithMessage("Name cannot be empty.")
                .MaximumLength(200).WithMessage("Stock item name must not exceed 200 characters.");
        });

        When(x => x.Request.Barcode is not null, () =>
        {
            RuleFor(x => x.Request.Barcode)
                .MaximumLength(100).WithMessage("Barcode must not exceed 100 characters.");
        });

        When(x => x.Request.Type.HasValue, () =>
        {
            RuleFor(x => x.Request.Type!.Value)
                .IsInEnum().WithMessage("Invalid stock item type.");
        });

        When(x => x.Request.Unit.HasValue, () =>
        {
            RuleFor(x => x.Request.Unit!.Value)
                .IsInEnum().WithMessage("Invalid unit of measure.");
        });

        When(x => x.Request.CategoryId.HasValue, () =>
        {
            RuleFor(x => x.Request.CategoryId!.Value)
                .GreaterThan(0).WithMessage("CategoryId must be greater than 0.");
        });
    }
}