using FluentValidation;

namespace Application.MenuCategories.Commands.Create;

public class CreateMenuCategoryCommandValidator : AbstractValidator<CreateMenuCategoryCommand>
{
    public CreateMenuCategoryCommandValidator()
    {
        

        RuleFor(x => x.Request.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Request.Description)
            .MaximumLength(500);
    }
}