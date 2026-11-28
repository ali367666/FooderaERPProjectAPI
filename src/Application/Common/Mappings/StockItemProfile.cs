using Application.StockItem.Dtos.Request;
using Application.StockItem.Dtos.Response;
using AutoMapper;

namespace Application.StockItem.Mappings;

public class StockItemProfile : Profile
{
    public StockItemProfile()
    {
        CreateMap<StockItemRequest, Domain.Entities.WarehouseAndStock.StockItem>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive ?? true))
            .ForMember(dest => dest.Description,
                opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Description) ? null : src.Description.Trim()))
            .ForMember(dest => dest.MinimumStock,
                opt => opt.MapFrom(src => src.MinimumStock == null || src.MinimumStock < 0 ? 0 : src.MinimumStock.Value));

        CreateMap<Domain.Entities.WarehouseAndStock.StockItem, StockItemResponse>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category.Name))
            ;
    }
}