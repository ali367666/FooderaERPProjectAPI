using Application.Company.Dtos.Responce;
using AutoMapper;

namespace Application.Common.Mappings;

public class CompanyMappingProfile : Profile
{
    public CompanyMappingProfile()
    {
        CreateMap<Domain.Entities.Company, GetCompanyByIdResponse>();
        CreateMap<Domain.Entities.Company, GetAllCompaniesResponse>();
        CreateMap<Domain.Entities.Company, CreateCompanyResponse>();
    }
}
