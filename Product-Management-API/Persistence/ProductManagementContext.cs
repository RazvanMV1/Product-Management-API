using Microsoft.EntityFrameworkCore;
using Product_Management_API.Features.Products;

namespace Product_Management_API.Persistence;

public class ProductManagementContext(DbContextOptions<ProductManagementContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
}