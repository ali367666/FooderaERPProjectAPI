using Application.StockCategory.Dtos.Response;
using AutoMapper;
using Domain.Entities;

namespace Application.Common.Mappings;

public class StockCategoryMappingProfile : Profile
{
    public StockCategoryMappingProfile()
    {
        CreateMap<Domain.Entities.StockCategory, StockCategoryResponse>()
            .ForMember(dest => dest.CompanyName,
                opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null))
            .ForMember(dest => dest.ParentName,
                opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null));

        CreateMap<Domain.Entities.StockCategory, CreateStockCategoryResponse>();

        CreateMap<Domain.Entities.StockCategory, UpdateStockCategoryResponse>();

        CreateMap<Domain.Entities.StockCategory, StockCategoryChildResponse>();

        CreateMap<Domain.Entities.StockCategory, StockCategoryDetailResponse>()
            .ForMember(dest => dest.CompanyName,
                opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null))
            .ForMember(dest => dest.ParentName,
                opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null))
            .ForMember(dest => dest.Children,
                opt => opt.MapFrom(src => src.Children));
    }
}