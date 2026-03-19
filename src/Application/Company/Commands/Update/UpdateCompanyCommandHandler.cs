using Application.Common.Extensions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Commands.Update;

public sealed class UpdateCompanyCommandHandler
    : IRequestHandler<UpdateCompanyCommand, BaseResponse<UpdateCompanyResponse>>
{
    private readonly ICompanyRepository _repository;

    public UpdateCompanyCommandHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaseResponse<UpdateCompanyResponse>> Handle(
        UpdateCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
            return BaseResponse<UpdateCompanyResponse>.Fail("Şirkət tapılmadı.");

        var dto = request.dto;

        if (!string.IsNullOrWhiteSpace(dto.CompanyCode))
            company.CompanyCode = dto.CompanyCode;

        if (!string.IsNullOrWhiteSpace(dto.Name))
            company.Name = dto.Name;

        if (dto.Description is not null)
            company.Description = dto.Description;

        if (dto.Address is not null)
            company.Address = dto.Address;

        if (!string.IsNullOrWhiteSpace(dto.TaxNumber))
            company.TaxNumber = dto.TaxNumber;

        if (!string.IsNullOrWhiteSpace(dto.TaxOfficeCode))
            company.TaxOfficeCode = dto.TaxOfficeCode;

        if (dto.Country.HasValue)
        {
            company.Country = dto.Country.Value;
            company.CountryCode = dto.Country.Value.GetCode();
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var response = new UpdateCompanyResponse
        {
            Id = company.Id,
            Message = "Şirkət uğurla yeniləndi"
        };

        return BaseResponse<UpdateCompanyResponse>.Ok(response, "Şirkət uğurla yeniləndi");
    }
}