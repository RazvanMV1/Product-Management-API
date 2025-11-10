using AutoMapper;

namespace Product_Management_API.Common.Mapping.Resolvers;

public class AvailabilityStatusResolver : IValueResolver<Features.Products.Product, Features.Products.ProductProfileDto, string>
{
    public string Resolve(Features.Products.Product src, Features.Products.ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        if (!src.IsAvailable)
        {
            return "Out of Stock";
        }

        return src.StockQuantity switch
        {
            0 => "Unavailable",
            1 => "Last Item",
            <= 5 => "Limited Stock",
            _ => "In Stock"
        };
    }
}