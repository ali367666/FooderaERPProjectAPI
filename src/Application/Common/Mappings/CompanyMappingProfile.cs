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

        CreateMap<Domain.Entities.Company, GetCompanyByCodeResponse>()
            .ForMember(d => d.CompanyId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Name));
        CreateMap<Domain.Entities.Restaurant, GetCompanyByCodeResponse.RestaurantLookupItem>();
    }
}
