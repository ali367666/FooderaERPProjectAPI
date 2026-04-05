using FluentValidation;

namespace Application.WarehouseTransfer.Commands.Submit;

public class SubmitWarehouseTransferCommandValidator
    : AbstractValidator<SubmitWarehouseTransferCommand>
{
    public SubmitWarehouseTransferCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}