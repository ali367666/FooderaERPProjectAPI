using Application.CompanySettings.Dtos;
using AutoMapper;

namespace Application.Common.Mappings;

public class CompanySettingsMappingProfile : Profile
{
    public CompanySettingsMappingProfile()
    {
        CreateMap<Domain.Entities.CompanySettings, CompanySettingsResponse>();
    }
}
