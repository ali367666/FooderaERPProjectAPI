using FluentValidation;

namespace Application.Positions.Commands.Create;

public class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        // 0 = no department chosen: allowed only for a company without the Departments module (the handler refuses it otherwise).
        RuleFor(x => x.Request.DepartmentId)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DepartmentId must not be negative.");

        RuleFor(x => x.Request.Name)
            .NotEmpty()
            .WithMessage("Position name is required.")
            .MaximumLength(100)
            .WithMessage("Position name cannot exceed 100 characters.");
    }
}