using FluentValidation;

namespace Application.Warehouse.Commands.Delete;

public class DeleteWarehouseCommandValidator : AbstractValidator<DeleteWarehouseCommand>
{
    public DeleteWarehouseCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Warehouse id must be greater than 0.");
    }
}