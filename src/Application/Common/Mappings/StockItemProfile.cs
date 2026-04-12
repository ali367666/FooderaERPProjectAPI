using Application.StockItem.Dtos.Request;
using Application.StockItem.Dtos.Response;
using AutoMapper;

namespace Application.StockItem.Mappings;

public class StockItemProfile : Profile
{
    public StockItemProfile()
    {
        CreateMap<StockItemRequest, Domain.Entities.WarehouseAndStock.StockItem>();

        CreateMap<Domain.Entities.WarehouseAndStock.StockItem, StockItemResponse>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category.Name))
            ;
    }
}