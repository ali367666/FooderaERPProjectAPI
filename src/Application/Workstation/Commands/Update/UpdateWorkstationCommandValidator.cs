using FluentValidation;

namespace Application.Workstation.Commands.Update;

public class UpdateWorkstationCommandValidator : AbstractValidator<UpdateWorkstationCommand>
{
    public UpdateWorkstationCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id 0-dan böyük olmalıdır.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Terminalın adı boş ola bilməz.")
            .MaximumLength(100).WithMessage("Terminalın adı ən çox 100 simvol ola bilər.");

        RuleFor(x => x.Request.RestaurantId)
            .GreaterThan(0).WithMessage("RestaurantId 0-dan böyük olmalıdır.")
            .When(x => x.Request.RestaurantId.HasValue);
    }
}
