using FluentValidation;

namespace Application.WarehouseTransfer.Commands.Receive;

public class ReceiveWarehouseTransferCommandValidator
    : AbstractValidator<ReceiveWarehouseTransferCommand>
{
    public ReceiveWarehouseTransferCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}