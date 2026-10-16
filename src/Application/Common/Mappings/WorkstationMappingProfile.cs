using Application.Workstation.Dtos.Responce;
using AutoMapper;

namespace Application.Common.Mappings;

public class WorkstationMappingProfile : Profile
{
    public WorkstationMappingProfile()
    {
        CreateMap<Domain.Entities.Workstation, WorkstationResponse>()
            .ForMember(d => d.RestaurantName, o => o.MapFrom(s => s.Restaurant != null ? s.Restaurant.Name : null));
    }
}
