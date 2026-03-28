using FluentValidation;

namespace Application.StockItem.Commands.Update;

public class UpdateStockItemCommandValidator : AbstractValidator<UpdateStockItemCommand>
{
    public UpdateStockItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Stock item name is required.")
            .MaximumLength(200).WithMessage("Stock item name must not exceed 200 characters.");

        RuleFor(x => x.Request.CategoryId)
            .GreaterThan(0).WithMessage("CategoryId must be greater than 0.");

        RuleFor(x => x.Request.CompanyId)
            .GreaterThan(0).WithMessage("CompanyId must be greater than 0.");

        RuleFor(x => x.Request.Type)
            .IsInEnum().WithMessage("Invalid stock item type.");

        RuleFor(x => x.Request.Unit)
            .IsInEnum().WithMessage("Invalid unit of measure.");

        RuleFor(x => x.Request.Barcode)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Barcode))
            .WithMessage("Barcode must not exceed 100 characters.");
    }
}