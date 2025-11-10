using AutoMapper;

namespace Product_Management_API.Common.Mapping.Resolvers;

public class CategoryDisplayResolver : IValueResolver<Features.Products.Product,Features.Products.ProductProfileDto,string>
{
    public string Resolve(Features.Products.Product src, Features.Products.ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        return src.Category switch
        {
            Features.Products.ProductCategory.Electronics => "Electronics & Technology",
            Features.Products.ProductCategory.Clothing => "Clothing & Fashion",
            Features.Products.ProductCategory.Books => "Books & Media",
            Features.Products.ProductCategory.Home => "Home & Garden",
            _ => "Uncategorized"
        };
    }
}