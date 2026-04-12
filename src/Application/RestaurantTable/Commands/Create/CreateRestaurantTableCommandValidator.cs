using FluentValidation;

namespace Application.RestaurantTables.Commands.Create;

public class CreateRestaurantTableCommandValidator : AbstractValidator<CreateRestaurantTableCommand>
{
    public CreateRestaurantTableCommandValidator()
    {
        RuleFor(x => x.Request.RestaurantId)
            .GreaterThan(0).WithMessage("RestaurantId 0-dan böyük olmalıdır.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Masa adı boş ola bilməz.")
            .MaximumLength(100).WithMessage("Masa adı 100 simvoldan çox ola bilməz.");

        RuleFor(x => x.Request.Capacity)
            .GreaterThan(0).WithMessage("Tutum 0-dan böyük olmalıdır.");
    }
}