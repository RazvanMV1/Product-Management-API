using AutoMapper;

namespace Product_Management_API.Common.Mapping.Resolvers;

public class PriceFormatterResolver : IValueResolver<Features.Products.Product, Features.Products.ProductProfileDto, string>
{
    public string Resolve(Features.Products.Product src, Features.Products.ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        // Calculează prețul după discount (dacă e Home)
        var finalPrice = src.Category == Features.Products.ProductCategory.Home 
            ? Math.Round(src.Price * 0.9m, 2) 
            : src.Price;
            
        return finalPrice.ToString("C2");
    }
}