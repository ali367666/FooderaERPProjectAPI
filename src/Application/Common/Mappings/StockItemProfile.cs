using Application.StockItem.Dtos.Request;
using Application.StockItem.Dtos.Response;
using AutoMapper;
using Domain.Entities;

namespace Application.StockItem.Mappings;

public class StockItemProfile : Profile
{
    public StockItemProfile()
    {
        CreateMap<StockItemRequest, Domain.Entities.StockItem>();

        CreateMap<Domain.Entities.StockItem, StockItemResponse>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category.Name))
            ;
    }
}