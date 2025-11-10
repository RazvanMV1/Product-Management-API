using AutoMapper;

namespace Product_Management_API.Common.Mapping.Resolvers;

public class BrandInitialsResolver : IValueResolver<Features.Products.Product, Features.Products.ProductProfileDto, string>
{
    public string Resolve(Features.Products.Product src, Features.Products.ProductProfileDto dest, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(src.Brand))
            return "?";

        var words = src.Brand.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 1)
            return words[0][0].ToString().ToUpper();

        var firstInitial = char.ToUpper(words.First()[0]);
        var lastInitial = char.ToUpper(words.Last()[0]);

        return $"{firstInitial}{lastInitial}";
        
    }
}