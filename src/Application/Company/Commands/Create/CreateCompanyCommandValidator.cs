using FluentValidation;

namespace Application.Company.Commands.Create;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.dto.CompanyCode)
            .NotEmpty().WithMessage("CompanyCode boş ola bilməz.")
            .MaximumLength(50).WithMessage("CompanyCode ən çox 50 simvol ola bilər.");

        RuleFor(x => x.dto.Name)
            .NotEmpty().WithMessage("Name boş ola bilməz.")
            .MaximumLength(150).WithMessage("Name ən çox 150 simvol ola bilər.");

        RuleFor(x => x.dto.Description)
            .MaximumLength(500).WithMessage("Description ən çox 500 simvol ola bilər.");

        RuleFor(x => x.dto.Address)
            .MaximumLength(500).WithMessage("Address ən çox 500 simvol ola bilər.");

        RuleFor(x => x.dto.TaxOfficeCode)
            .MaximumLength(50).WithMessage("TaxOfficeCode ən çox 50 simvol ola bilər.");

        RuleFor(x => x.dto.TaxNumber)
            .MaximumLength(50).WithMessage("TaxNumber ən çox 50 simvol ola bilər.");

        RuleFor(x => x.dto.CountryCode)
            .MaximumLength(20).WithMessage("CountryCode ən çox 20 simvol ola bilər.");

        RuleFor(x => x.dto.CountryName)
            .MaximumLength(100).WithMessage("CountryName ən çox 100 simvol ola bilər.");

        RuleFor(x => x.dto.PrimaryPhoneNumber)
            .MaximumLength(30).WithMessage("PrimaryPhoneNumber ən çox 30 simvol ola bilər.");

        RuleFor(x => x.dto.SecondaryPhoneNumber)
            .MaximumLength(30).WithMessage("SecondaryPhoneNumber ən çox 30 simvol ola bilər.");

        RuleFor(x => x.dto.Email)
            .MaximumLength(256).WithMessage("Email ən çox 256 simvol ola bilər.");

        RuleFor(x => x.dto.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.dto.Email))
            .WithMessage("Email formatı düzgün deyil.");
    }
}