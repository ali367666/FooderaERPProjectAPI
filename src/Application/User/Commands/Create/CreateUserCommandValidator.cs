using Domain.Enums;
using FluentValidation;

namespace Application.User.Commands.Create;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.dto.EmployeeId)
            .GreaterThan(0)
            .WithMessage("EmployeeId düzgün daxil edilməlidir.");

        RuleFor(x => x.dto.UserName)
            .NotEmpty()
            .WithMessage("UserName boş ola bilməz.")
            .MaximumLength(50)
            .WithMessage("UserName maksimum 50 simvol ola bilər.");

        RuleFor(x => x.dto.Email)
            .NotEmpty()
            .WithMessage("Email boş ola bilməz.")
            .EmailAddress()
            .WithMessage("Email formatı düzgün deyil.");

        RuleFor(x => x.dto.Password)
            .NotEmpty()
            .WithMessage("Password boş ola bilməz.")
            .MinimumLength(6)
            .WithMessage("Password minimum 6 simvol olmalıdır.");

        RuleFor(x => x.dto.RestaurantId)
            .Null()
            .When(x => x.dto.WorkplaceType == EmployeeWorkplaceType.HeadOffice)
            .WithMessage("HeadOffice üçün RestaurantId boş olmalıdır.");

        RuleFor(x => x.dto.RestaurantId)
            .NotNull()
            .When(x => x.dto.WorkplaceType == EmployeeWorkplaceType.Restaurant)
            .WithMessage("Restaurant workplace üçün RestaurantId mütləq verilməlidir.");
    }
}