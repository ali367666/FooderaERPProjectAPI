using Application.StockCategory.Commands.Update;
using FluentValidation;

namespace Application.StockCategory.Validators;

public class UpdateStockCategoryCommandValidator : AbstractValidator<UpdateStockCategoryCommand>
{
    public UpdateStockCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Valid category id is required.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Description));

        RuleFor(x => x.Request.CompanyId)
            .GreaterThan(0).WithMessage("Valid company id is required.");

        RuleFor(x => x.Request.ParentId)
            .GreaterThan(0).WithMessage("Parent id must be greater than 0.")
            .When(x => x.Request.ParentId.HasValue);

        RuleFor(x => x)
            .Must(x => x.Request.ParentId != x.Id)
            .WithMessage("A category cannot be its own parent.");
    }
}