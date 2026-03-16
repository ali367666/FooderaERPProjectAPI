using Application.Common.Extensions;
using Application.Company.Dtos.Responce;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Company.Commands.Update;

public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, UpdateCompanyResponse>
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;

    public UpdateCompanyCommandHandler(ICompanyRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<UpdateCompanyResponse> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
            throw new Exception("Şirkət tapılmadı.");

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

        return new UpdateCompanyResponse
        {
            Id = company.Id,
            Message = "Şirkət uğurla yeniləndi"
        };
    
}
}