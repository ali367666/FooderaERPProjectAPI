using Application.Warehouse.Dtos.Response;
using AutoMapper;
using Domain.Entities;

namespace Application.Warehouse.Mappings;

public class WarehouseProfile : Profile
{
    public WarehouseProfile()
    {
        CreateMap<Domain.Entities.Warehouse, WarehouseResponse>()
            .ForMember(dest => dest.RestaurantName,
                opt => opt.MapFrom(src => src.Restaurant != null ? src.Restaurant.Name : null))
            .ForMember(dest => dest.ResponsibleEmployeeFullName,
                opt => opt.MapFrom(src => src.ResponsibleEmployee != null
                    ? $"{src.ResponsibleEmployee.FirstName} {src.ResponsibleEmployee.LastName}".Trim()
                    : null))
            .ForMember(dest => dest.DriverFullName,
                opt => opt.MapFrom(src => src.DriverUser != null
                    ? $"{src.DriverUser.FullName}  "
                    : null));
    }
}