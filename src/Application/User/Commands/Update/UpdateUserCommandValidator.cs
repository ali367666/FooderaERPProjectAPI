using Application.User.Commands.Update;
using Domain.Enums;
using FluentValidation;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.id)
            .GreaterThan(0)
            .WithMessage("User Id 0-dan böyük olmalıdır.");

        RuleFor(x => x.dto)
            .NotNull()
            .WithMessage("Request boş ola bilməz.");

        RuleFor(x => x.dto.FullName)
            .NotEmpty()
            .WithMessage("FullName boş ola bilməz.")
            .MaximumLength(100)
            .WithMessage("FullName maksimum 100 simvol ola bilər.");

        RuleFor(x => x.dto.CompanyId)
            .GreaterThan(0)
            .WithMessage("CompanyId 0-dan böyük olmalıdır.");

        RuleFor(x => x.dto.WorkplaceType)
            .IsInEnum()
            .WithMessage("WorkplaceType düzgün dəyər deyil.");

        RuleFor(x => x.dto.RestaurantId)
            .GreaterThan(0)
            .When(x => x.dto.RestaurantId.HasValue)
            .WithMessage("RestaurantId göndərilirsə, 0-dan böyük olmalıdır.");

        When(x => x.dto.WorkplaceType == EmployeeWorkplaceType.HeadOffice, () =>
        {
            RuleFor(x => x.dto.RestaurantId)
                .Null()
                .WithMessage("Company workplace üçün RestaurantId null olmalıdır.");
        });

        When(x => x.dto.WorkplaceType == EmployeeWorkplaceType.Restaurant, () =>
        {
            RuleFor(x => x.dto.RestaurantId)
                .NotNull()
                .WithMessage("Restaurant workplace üçün RestaurantId mütləq olmalıdır.");
        });
    }
}