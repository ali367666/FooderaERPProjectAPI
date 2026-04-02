using FluentValidation;

namespace Application.StockRequests.Commands.Recall;

public class RecallStockRequestCommandValidator : AbstractValidator<RecallStockRequestCommand>
{
    public RecallStockRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}