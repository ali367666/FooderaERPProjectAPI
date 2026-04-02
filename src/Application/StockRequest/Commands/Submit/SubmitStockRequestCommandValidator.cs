using FluentValidation;

namespace Application.StockRequests.Commands.Submit;

public class SubmitStockRequestCommandValidator : AbstractValidator<SubmitStockRequestCommand>
{
    public SubmitStockRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}