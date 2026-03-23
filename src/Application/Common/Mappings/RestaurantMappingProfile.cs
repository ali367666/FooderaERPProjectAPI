using Application.Restaurant.Dtos.Request;
using AutoMapper;
using Domain.Entities;

namespace Application.Common.Mappings;

public class RestaurantMappingProfile : Profile
{
    public RestaurantMappingProfile()
    {
        CreateMap<CreateRestaurantRequest, Domain.Entities.Restaurant>();
    }
}