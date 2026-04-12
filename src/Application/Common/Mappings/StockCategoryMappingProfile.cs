using Application.StockCategory.Dtos.Response;
using AutoMapper;

namespace Application.Common.Mappings;

public class StockCategoryMappingProfile : Profile
{
    public StockCategoryMappingProfile()
    {
        CreateMap<Domain.Entities.WarehouseAndStock.StockCategory, StockCategoryResponse>()
            .ForMember(dest => dest.CompanyName,
                opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null))
            .ForMember(dest => dest.ParentName,
                opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null));

        CreateMap<Domain.Entities.WarehouseAndStock.StockCategory, CreateStockCategoryResponse>();

        CreateMap<Domain.Entities.WarehouseAndStock.StockCategory, UpdateStockCategoryResponse>();

        CreateMap<Domain.Entities.WarehouseAndStock.StockCategory, StockCategoryChildResponse>();

        CreateMap<Domain.Entities.WarehouseAndStock.StockCategory, StockCategoryDetailResponse>()
            .ForMember(dest => dest.CompanyName,
                opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null))
            .ForMember(dest => dest.ParentName,
                opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null))
            .ForMember(dest => dest.Children,
                opt => opt.MapFrom(src => src.Children));
    }
}