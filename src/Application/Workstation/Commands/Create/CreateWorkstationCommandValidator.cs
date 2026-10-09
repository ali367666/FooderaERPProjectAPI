using FluentValidation;

namespace Application.Workstation.Commands.Create;

public class CreateWorkstationCommandValidator : AbstractValidator<CreateWorkstationCommand>
{
    public CreateWorkstationCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Terminalın adı boş ola bilməz.")
            .MaximumLength(100).WithMessage("Terminalın adı ən çox 100 simvol ola bilər.");

        RuleFor(x => x.Request.RestaurantId)
            .GreaterThan(0).WithMessage("RestaurantId 0-dan böyük olmalıdır.")
            .When(x => x.Request.RestaurantId.HasValue);
    }
}
