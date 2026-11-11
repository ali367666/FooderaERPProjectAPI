using FluentValidation;

namespace Application.Employees.Commands.Update;

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be greater than 0.");

        // The name is one field now: a last name is optional, the first word is enough.
        RuleFor(x => x.Request.FirstName)
            .NotEmpty()
            .WithMessage("Ad və soyad mütləqdir.")
            .MaximumLength(100);

        RuleFor(x => x.Request.LastName)
            .MaximumLength(100);

        // 0 = no department chosen: allowed only for a company without the Departments module (the handler refuses it otherwise).
        RuleFor(x => x.Request.DepartmentId)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DepartmentId must not be negative.");

        RuleFor(x => x.Request).Must(r => r.PositionId > 0 || r.RoleId is > 0)
            .WithMessage("Vəzifə (rol) seçin.");

        RuleFor(x => x.Request.HireDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Hire date cannot be in the future.");
    }
}
