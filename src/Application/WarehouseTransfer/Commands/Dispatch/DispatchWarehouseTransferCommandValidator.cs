using FluentValidation;

namespace Application.WarehouseTransfer.Commands.Dispatch;

public class DispatchWarehouseTransferCommandValidator
    : AbstractValidator<DispatchWarehouseTransferCommand>
{
    public DispatchWarehouseTransferCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}