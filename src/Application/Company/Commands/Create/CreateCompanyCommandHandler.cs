using Application.Common.Extensions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using AutoMapper;
using MediatR;

namespace Application.Company.Commands.Create;

public sealed class CreateCompanyCommandHandler
    : IRequestHandler<CreateCompanyCommand, BaseResponse<CreateCompanyResponse>>
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;

    public CreateCompanyCommandHandler(ICompanyRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<BaseResponse<CreateCompanyResponse>> Handle(
        CreateCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var companyCodeExists = await _repository.AnyAsync(
            x => x.CompanyCode == dto.CompanyCode,
            cancellationToken);

        if (companyCodeExists)
            return BaseResponse<CreateCompanyResponse>.Fail("Bu company code artıq mövcuddur.");

        var company = _mapper.Map<Domain.Entities.Company>(dto);
        company.CountryCode = dto.Country.GetCode();

        await _repository.AddAsync(company, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = new CreateCompanyResponse
        {
            Id = company.Id,
            Message = "Şirkət uğurla yaradıldı"
        };

        return BaseResponse<CreateCompanyResponse>.Ok(response, "Şirkət uğurla yaradıldı");
    }
}