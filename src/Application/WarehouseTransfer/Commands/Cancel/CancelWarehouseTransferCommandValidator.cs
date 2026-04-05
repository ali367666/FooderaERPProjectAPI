using FluentValidation;

namespace Application.WarehouseTransfer.Commands.Cancel;

public class CancelWarehouseTransferCommandValidator
    : AbstractValidator<CancelWarehouseTransferCommand>
{
    public CancelWarehouseTransferCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}