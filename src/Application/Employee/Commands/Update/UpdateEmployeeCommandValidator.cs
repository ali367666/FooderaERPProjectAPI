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

        RuleFor(x => x.Request.DepartmentId)
            .GreaterThan(0);

        RuleFor(x => x.Request.PositionId)
            .GreaterThan(0);

        RuleFor(x => x.Request.HireDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Hire date cannot be in the future.");
    }
}
