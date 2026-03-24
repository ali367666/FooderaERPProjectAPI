using FluentValidation;

public class UpdateRestaurantCommandValidator : AbstractValidator<UpdateRestaurantCommand>
{
    public UpdateRestaurantCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Restaurant id must be greater than 0.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Restaurant name is required.")
            .MaximumLength(100).WithMessage("Restaurant name must not exceed 100 characters.");

        RuleFor(x => x.Request.CompanyId)
            .GreaterThan(0).WithMessage("Company id must be greater than 0.");

        RuleFor(x => x.Request.Phone)
            .MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.Request.Phone));

        RuleFor(x => x.Request.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Request.Email));
    }
}