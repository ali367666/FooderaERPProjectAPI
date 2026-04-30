using Application.Departments.Dtos;
using FluentValidation;

namespace Application.Departments.Commands.Create;

public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("Valid companyId is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Department adı boş ola bilməz.")
            .MaximumLength(100)
            .WithMessage("Department adı 100 simvoldan çox ola bilməz.");

        RuleFor(x => x.Description)
            .MaximumLength(250)
            .WithMessage("Description 250 simvoldan çox ola bilməz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}