using FluentValidation;

namespace Application.Employees.Commands.Create;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Request.DepartmentId).GreaterThan(0);
        RuleFor(x => x.Request.PositionId).GreaterThan(0);

        RuleFor(x => x.Request.HireDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Hire date cannot be in the future.");
    }
}