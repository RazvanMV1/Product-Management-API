using AutoMapper;
using Product_Management_API.Features.Products;
using Product_Management_API.Common.Mapping.Resolvers;

namespace Product_Management_API.Common.Mapping;

public class AdvancedProductMappingProfile: Profile
{
    public AdvancedProductMappingProfile()
    {
        CreateMap<Features.Products.CreateProductProfileRequest, Features.Products.Product>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest=> dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest=> dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        CreateMap<Features.Products.Product, Features.Products.ProductProfileDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
            .ForMember(dest => dest.SKU, opt => opt.MapFrom(src => src.SKU))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.IsAvailable))
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity))

            // Custom Resolvers
            .ForMember(dest => dest.CategoryDisplayName,
                opt => opt.MapFrom<CategoryDisplayResolver>())
            .ForMember(dest => dest.FormattedPrice,
                opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.ProductAge,
                opt => opt.MapFrom<ProductAgeResolver>())
            .ForMember(dest => dest.BrandInitials,
                opt => opt.MapFrom<BrandInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus,
                opt => opt.MapFrom<AvailabilityStatusResolver>())

            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src
                => src.Category == Features.Products.ProductCategory.Home ? null : src.ImageUrl))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Category == Features.Products.ProductCategory.Home ? src.Price * 0.9m : src.Price));

    }
}