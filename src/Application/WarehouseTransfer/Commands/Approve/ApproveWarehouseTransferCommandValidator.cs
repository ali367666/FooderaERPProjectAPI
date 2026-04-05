using FluentValidation;

namespace Application.WarehouseTransfer.Commands.Approve;

public class ApproveWarehouseTransferCommandValidator
    : AbstractValidator<ApproveWarehouseTransferCommand>
{
    public ApproveWarehouseTransferCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}