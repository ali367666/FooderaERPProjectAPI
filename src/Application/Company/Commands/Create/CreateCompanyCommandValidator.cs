using FluentValidation;

namespace Application.Company.Commands.Create;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Request.CompanyCode)
            .NotEmpty().WithMessage("CompanyCode boş ola bilməz.")
            .MaximumLength(50).WithMessage("CompanyCode ən çox 50 simvol ola bilər.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name boş ola bilməz.")
            .MaximumLength(150).WithMessage("Name ən çox 150 simvol ola bilər.");

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("Description ən çox 500 simvol ola bilər.");

        RuleFor(x => x.Request.Address)
            .MaximumLength(500).WithMessage("Address ən çox 500 simvol ola bilər.");

        RuleFor(x => x.Request.TaxOfficeCode)
            .MaximumLength(50).WithMessage("TaxOfficeCode ən çox 50 simvol ola bilər.");

        RuleFor(x => x.Request.TaxNumber)
            .MaximumLength(50).WithMessage("TaxNumber ən çox 50 simvol ola bilər.");

        RuleFor(x => x.Request.CountryCode)
            .MaximumLength(20).WithMessage("CountryCode ən çox 20 simvol ola bilər.");

        RuleFor(x => x.Request.CountryName)
            .MaximumLength(100).WithMessage("CountryName ən çox 100 simvol ola bilər.");

        RuleFor(x => x.Request.PrimaryPhoneNumber)
            .MaximumLength(30).WithMessage("PrimaryPhoneNumber ən çox 30 simvol ola bilər.");

        RuleFor(x => x.Request.SecondaryPhoneNumber)
            .MaximumLength(30).WithMessage("SecondaryPhoneNumber ən çox 30 simvol ola bilər.");

        RuleFor(x => x.Request.Email)
            .MaximumLength(256).WithMessage("Email ən çox 256 simvol ola bilər.");

        RuleFor(x => x.Request.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Email))
            .WithMessage("Email formatı düzgün deyil.");
    }
}