using FluentValidation;

namespace Application.Positions.Commands.Create;

public class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(x => x.Request.DepartmentId)
            .GreaterThan(0)
            .WithMessage("DepartmentId must be greater than 0.");

        RuleFor(x => x.Request.Name)
            .NotEmpty()
            .WithMessage("Position name is required.")
            .MaximumLength(100)
            .WithMessage("Position name cannot exceed 100 characters.");
    }
}