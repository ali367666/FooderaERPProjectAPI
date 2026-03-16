using FluentValidation;

namespace Application.Company.Commands.Update;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id 0-dan böyük olmalıdır.");

        RuleFor(x => x.dto.CompanyCode)
            .NotEmpty()
            .WithMessage("Şirkət kodu boş ola bilməz.")
            .Length(3)
            .WithMessage("Şirkət kodu tam 3 simvol olmalıdır.")
            .Matches("^[A-Z]{3}$")
            .WithMessage("Şirkət kodu yalnız 3 böyük hərfdən ibarət olmalıdır.");

        RuleFor(x => x.dto.Name)
            .NotEmpty()
            .WithMessage("Şirkət adı boş ola bilməz.")
            .MinimumLength(3)
            .WithMessage("Şirkət adı minimum 3 simvol olmalıdır.")
            .MaximumLength(200)
            .WithMessage("Şirkət adı maksimum 200 simvol ola bilər.");

        RuleFor(x => x.dto.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.dto.Description))
            .WithMessage("Açıqlama maksimum 500 simvol ola bilər.");

        RuleFor(x => x.dto.Address)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.dto.Address))
            .WithMessage("Ünvan maksimum 500 simvol ola bilər.");

        RuleFor(x => x.dto.TaxNumber)
            .NotEmpty()
            .WithMessage("Vergi nömrəsi boş ola bilməz.")
            .MaximumLength(50)
            .WithMessage("Vergi nömrəsi maksimum 50 simvol ola bilər.");

        RuleFor(x => x.dto.TaxOfficeCode)
            .NotEmpty()
            .WithMessage("Vergi idarə kodu boş ola bilməz.")
            .MaximumLength(50)
            .WithMessage("Vergi idarə kodu maksimum 50 simvol ola bilər.");

        RuleFor(x => x.dto.Country)
            .IsInEnum()
            .WithMessage("Düzgün ölkə seçilməlidir.");
    }
}