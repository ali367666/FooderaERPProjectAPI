using FluentValidation;

namespace Application.StockRequests.Commands.Reject;

public class RejectStockRequestCommandValidator : AbstractValidator<RejectStockRequestCommand>
{
    public RejectStockRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}