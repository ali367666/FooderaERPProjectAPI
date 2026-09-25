using Domain.Enums;
using FluentValidation;

namespace Application.MenuItems.Commands.Create;

public class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Request.Description)
            .MaximumLength(1000);

        RuleFor(x => x.Request.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Request.Portion)
            .MaximumLength(100);

        RuleFor(x => x.Request.MenuCategoryId)
            .GreaterThan(0);
        RuleFor(x => x.Request.PreparationType)
            .IsInEnum().WithMessage("PreparationType düzgün seçilməlidir.");

        // SET (bundle) items have no barcode of their own, and weight-sold items are identified
        // by their auto-generated weight-code sticker instead — everything else must have one.
        RuleFor(x => x.Request.Barcode)
            .NotEmpty()
            .WithMessage("Barkod məcburidir.")
            .When(x => !x.Request.IsSet
                && x.Request.UnitId != (int)UnitOfMeasure.Kg
                && x.Request.UnitId != (int)UnitOfMeasure.Gram);
    }
}