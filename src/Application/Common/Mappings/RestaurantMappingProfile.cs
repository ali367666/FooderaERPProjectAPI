using Application.Restaurant.Dtos.Request;
using Application.Restaurant.Dtos.Responce;
using AutoMapper;
using Domain.Entities;

namespace Application.Common.Mappings;

public class RestaurantMappingProfile : Profile
{
    public RestaurantMappingProfile()
    {
        CreateMap<CreateRestaurantRequest, Domain.Entities.Restaurant>();
        CreateMap<UpdateRestaurantRequest, Domain.Entities.Restaurant>();

        CreateMap<Domain.Entities.Restaurant, CreateRestaurantResponse>();
        CreateMap<Domain.Entities.Restaurant, GetRestaurantByIdResponce>();
        CreateMap<Domain.Entities.Restaurant, GetAllRestaurantResponce>();
        CreateMap<Domain.Entities.Restaurant, RestaurantResponse>();
    }
}