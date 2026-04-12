using FluentValidation;

namespace Application.MenuCategories.Commands.Update;

public class UpdateMenuCategoryCommandValidator : AbstractValidator<UpdateMenuCategoryCommand>
{
    public UpdateMenuCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.CompanyId)
            .GreaterThan(0);

        RuleFor(x => x.Request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Request.Description)
            .MaximumLength(500);
    }
}