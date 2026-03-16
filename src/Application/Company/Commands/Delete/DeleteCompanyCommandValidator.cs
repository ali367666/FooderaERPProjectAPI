using Application.Company.Commands.Delete;
using FluentValidation;

public class DeleteCompanyCommandValidator : AbstractValidator<DeleteCompanyCommand>
{
    public DeleteCompanyCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Company Id 0-dan böyük olmalıdır.");
    }
}