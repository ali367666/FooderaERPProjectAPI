using Application.Company.Dtos.Request;
using Application.Company.Dtos.Responce;
using AutoMapper;

namespace Application.Common.Mappings;

public class CompanyMappingProfile : Profile
{
    public CompanyMappingProfile()
    {
        CreateMap<CreateCompanyRequest, Domain.Entities.Company>();

        CreateMap<Domain.Entities.Company, GetCompanyByIdResponse>();
        CreateMap<Domain.Entities.Company, GetAllCompaniesResponse>();
        CreateMap<Domain.Entities.Company, CreateCompanyResponse>();
    }
}
