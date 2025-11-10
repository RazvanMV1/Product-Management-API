using AutoMapper;
 
 namespace Product_Management_API.Common.Mapping.Resolvers;
 
 public class PriceFormatterResolver : IValueResolver<Features.Products.Product, Features.Products.ProductProfileDto, string>
 {
     public string Resolve(Features.Products.Product src, Features.Products.ProductProfileDto dest, string destMember, ResolutionContext context)
     {
         return src.Price.ToString("C2");
     }
 }