using AutoMapper;

namespace Product_Management_API.Common.Mapping.Resolvers;

public class ProductAgeResolver : IValueResolver<Features.Products.Product, Features.Products.ProductProfileDto, string>
{
    public string Resolve(Features.Products.Product source, Features.Products.ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        var ageInDays = (DateTime.UtcNow - source.ReleaseDate).TotalDays;

        return ageInDays switch
        {
            < 30 => "New Release",
            < 365 => $"{(int)(ageInDays / 30)} month{((int)(ageInDays / 30) == 1 ? "" : "s")} old",
            < 1825 => $"{(int)(ageInDays / 365)} year{((int)(ageInDays / 365) == 1 ? "" : "s")} old",
            _ => "Classic"
        };
    }
}