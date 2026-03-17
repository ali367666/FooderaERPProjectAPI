using FluentValidation;
using Domain.Enums;

namespace Application.User.Commands.Create;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.dto.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.dto.UserName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.dto.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.dto.Password)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(x => x.dto.CompanyId)
            .GreaterThan(0);

        RuleFor(x => x.dto)
            .Must(x =>
                (x.WorkplaceType == EmployeeWorkplaceType.HeadOffice && x.RestaurantId == null) ||
                (x.WorkplaceType == EmployeeWorkplaceType.Restaurant && x.RestaurantId != null)
            )
            .WithMessage("WorkplaceType və RestaurantId uyğun deyil.");
    }
}