namespace Product_Management_API.Features.Products;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Brand { get; set; } = default!;
    public string SKU { get; set; } = default!;
    public ProductCategory Category { get; set; }
    public decimal Price { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Product()
    {
        IsAvailable = StockQuantity > 0;
    }
}