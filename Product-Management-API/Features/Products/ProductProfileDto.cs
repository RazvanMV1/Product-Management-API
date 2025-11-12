namespace Product_Management_API.Features.Products;

public class ProductProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Brand { get; set; } = default!;
    public string SKU { get; set; } = default!;
    public string CategoryDisplayName { get; set; } = default!;
    public decimal Price { get; set; }
    public string FormattedPrice { get; set; } = default!;
    public DateTime ReleaseDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
    public string ProductAge { get; set; } = default!;
    public string BrandInitials { get; set; } = default!;
    public string AvailabilityStatus { get; set; } = default!;
}