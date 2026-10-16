using FluentValidation;

namespace Application.Counterparty.Commands;

public class AddCounterpartyDebtCommandValidator : AbstractValidator<AddCounterpartyDebtCommand>
{
    public AddCounterpartyDebtCommandValidator()
    {
        RuleFor(x => x.Request.Amount)
            .GreaterThan(0).WithMessage("Əlavə olunan borc 0-dan böyük olmalıdır.");

        RuleFor(x => x.Request.Note)
            .MaximumLength(300).WithMessage("Qeyd ən çox 300 simvol ola bilər.");
    }
}
