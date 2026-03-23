using Application.Restaurant.Commands.Create;
using FluentValidation;

namespace Application.Restaurant.Validators;

public class CreateRestaurantCommandValidator : AbstractValidator<CreateRestaurantCommand>
{
    public CreateRestaurantCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Restaurant adı boş ola bilməz.")
            .MaximumLength(100).WithMessage("Restaurant adı ən çox 100 simvol ola bilər.");

        RuleFor(x => x.Request.Description)
            .MaximumLength(300).WithMessage("Description ən çox 300 simvol ola bilər.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Description));

        RuleFor(x => x.Request.Address)
            .MaximumLength(200).WithMessage("Address ən çox 200 simvol ola bilər.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Address));

        RuleFor(x => x.Request.Phone)
            .MaximumLength(20).WithMessage("Phone ən çox 20 simvol ola bilər.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Phone));

        RuleFor(x => x.Request.Email)
            .MaximumLength(128).WithMessage("Email ən çox 128 simvol ola bilər.")
            .EmailAddress().WithMessage("Email formatı düzgün deyil.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Email));

        RuleFor(x => x.Request.CompanyId)
            .GreaterThan(0).WithMessage("CompanyId 0-dan böyük olmalıdır.");
    }
}