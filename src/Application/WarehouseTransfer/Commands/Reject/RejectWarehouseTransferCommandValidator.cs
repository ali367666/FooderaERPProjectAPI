using FluentValidation;

namespace Application.WarehouseTransfer.Commands.Reject;

public class RejectWarehouseTransferCommandValidator
    : AbstractValidator<RejectWarehouseTransferCommand>
{
    public RejectWarehouseTransferCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}