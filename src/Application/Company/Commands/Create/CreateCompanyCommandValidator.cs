using FluentValidation;

namespace Application.Company.Commands.Create;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Request.CompanyCode)
            .NotEmpty().WithMessage("CompanyCode boş ola bilməz.")
            .MaximumLength(3).WithMessage("CompanyCode ən çox 3 simvol ola bilər.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Name boş ola bilməz.")
            .MaximumLength(50).WithMessage("Name ən çox 50 simvol ola bilər.");

        RuleFor(x => x.Request.Description)
            .MaximumLength(200).WithMessage("Description ən çox 200 simvol ola bilər.");

        RuleFor(x => x.Request.Address)
            .MaximumLength(200).WithMessage("Address ən çox 200 simvol ola bilər.");

        RuleFor(x => x.Request.TaxOfficeCode)
            .MaximumLength(50).WithMessage("TaxOfficeCode ən çox 50 simvol ola bilər.");

        RuleFor(x => x.Request.TaxNumber)
            .MaximumLength(50).WithMessage("TaxNumber ən çox 50 simvol ola bilər.");

        RuleFor(x => x.Request.CountryCode)
            .MaximumLength(3).WithMessage("CountryCode ən çox 3 simvol ola bilər.");

        RuleFor(x => x.Request.CountryName)
            .MaximumLength(50).WithMessage("CountryName ən çox 50 simvol ola bilər.");

        RuleFor(x => x.Request.PrimaryPhoneNumber)
            .MaximumLength(12).WithMessage("PrimaryPhoneNumber ən çox 12 simvol ola bilər.");

        RuleFor(x => x.Request.SecondaryPhoneNumber)
            .MaximumLength(12).WithMessage("SecondaryPhoneNumber ən çox 12 simvol ola bilər.");

        RuleFor(x => x.Request.Email)
            .MaximumLength(128).WithMessage("Email ən çox 128 simvol ola bilər.");

        RuleFor(x => x.Request.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Email))
            .WithMessage("Email formatı düzgün deyil.");
    }
}