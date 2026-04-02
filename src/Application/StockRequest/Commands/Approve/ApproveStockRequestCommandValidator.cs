using FluentValidation;

namespace Application.StockRequests.Commands.Approve;

public class ApproveStockRequestCommandValidator : AbstractValidator<ApproveStockRequestCommand>
{
    public ApproveStockRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}