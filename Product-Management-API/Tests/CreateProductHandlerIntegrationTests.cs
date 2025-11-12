using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Product_Management_API.Persistence;
using Product_Management_API.Features.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using FluentValidation;

namespace Product_Management_API.Tests;

/// <summary>
/// Integration tests for CreateProductHandler
/// Tests AutoMapper mappings, validation, business rules, and logging
/// Module 4 - Task 4.2: Product Integration Tests (100% PDF Compliant)
/// </summary>
public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ProductManagementContext _context;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerIntegrationTests()
    {
        var services = new ServiceCollection();

        // In-memory database with unique name per test run
        services.AddDbContext<ProductManagementContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Logging (real logger for integration tests)
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Memory cache
        services.AddMemoryCache();

        // AutoMapper with both product profiles
        services.AddAutoMapper(typeof(Program).Assembly);

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<Product_Management_API.Validators.CreateProductProfileValidator>();

        // Handler
        services.AddScoped<CreateProductHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ProductManagementContext>();
        _handler = _serviceProvider.GetRequiredService<CreateProductHandler>();
    }

    /// <summary>
    /// Test 1 (PDF): Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings
    /// Verifies: CategoryDisplayName, BrandInitials, ProductAge, FormattedPrice, AvailabilityStatus
    /// </summary>
    [Fact]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
        // Arrange
        var request = new CreateProductProfileRequest(
            Name: "Samsung Smart Galaxy Phone",
            Brand: "Samsung Electronics",  // Two-word brand for BrandInitials test
            SKU: "SAMS24U-512-BLK",
            Category: ProductCategory.Electronics,
            Price: 1299.99m,
            ReleaseDate: DateTime.UtcNow.AddMonths(-2),  // 2 months old for ProductAge
            ImageUrl: "https://example.com/galaxy-s24.jpg",
            StockQuantity: 8
        );

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Basic properties
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Samsung Smart Galaxy Phone", result.Name);
        Assert.Equal("SAMS24U-512-BLK", result.SKU);

        // Assert - PDF Requirement: CategoryDisplayName
        Assert.Equal("Electronics & Technology", result.CategoryDisplayName);

        // Assert - PDF Requirement: BrandInitials for two-word brand
        Assert.Equal("SE", result.BrandInitials); // Samsung Electronics → SE

        // Assert - PDF Requirement: ProductAge calculation
        Assert.Contains("month", result.ProductAge, StringComparison.OrdinalIgnoreCase);

        // Assert - PDF Requirement: FormattedPrice (culture-independent)
        Assert.NotEmpty(result.FormattedPrice);
        Assert.True(
            result.FormattedPrice.Contains("1299") || 
            result.FormattedPrice.Contains("1.299") || 
            result.FormattedPrice.Contains("1,299"),
            $"FormattedPrice '{result.FormattedPrice}' should contain formatted price 1299");

        // Assert - PDF Requirement: AvailabilityStatus based on stock
        Assert.Equal("In Stock", result.AvailabilityStatus); // Stock = 8 (> 5)

        // Verify product saved in database
        var savedProduct = await _context.Products.FirstOrDefaultAsync(p => p.SKU == request.SKU);
        Assert.NotNull(savedProduct);
        Assert.Equal(request.Name, savedProduct.Name);
    }

    /// <summary>
    /// Test 2 (PDF): Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging
    /// Verifies: ValidationException, error message
    /// </summary>
    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
    {
        // Arrange - Create existing product in database
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Gaming Laptop Original",
            Brand = "Original Brand",
            SKU = "ORIG-PROD-2025",
            Category = ProductCategory.Electronics,
            Price = 199.99m,
            ReleaseDate = new DateTime(2024, 1, 15),
            ImageUrl = "https://example.com/original.jpg",
            StockQuantity = 20,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(existingProduct);
        await _context.SaveChangesAsync();

        // Arrange - Create request with duplicate SKU
        var duplicateRequest = new CreateProductProfileRequest(
            Name: "Duplicate Product Book",
            Brand: "Duplicate Brand",
            SKU: "ORIG-PROD-2025", // Same SKU
            Category: ProductCategory.Books,
            Price: 29.99m,
            ReleaseDate: new DateTime(2024, 2, 1),
            ImageUrl: "https://example.com/duplicate.jpg",
            StockQuantity: 100
        );

        // Act & Assert - Verify ValidationException thrown
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(duplicateRequest, CancellationToken.None)
        );

        // Assert - PDF Requirement: Check exception message contains "already exists"
        Assert.NotNull(exception);
        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test 3 (PDF): Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping
    /// Verifies: Home category discount (10%), null ImageUrl, CategoryDisplayName
    /// </summary>
    [Fact]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {
        // Arrange
        var originalPrice = 100.00m;
        var request = new CreateProductProfileRequest(
            Name: "Garden Furniture Set",
            Brand: "HomeBrand",
            SKU: "HOME-GARDEN-01",
            Category: ProductCategory.Home,
            Price: originalPrice,
            ReleaseDate: new DateTime(2024, 6, 15),
            ImageUrl: "https://example.com/furniture.jpg", // Will be filtered to null
            StockQuantity: 15
        );

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - PDF Requirement: CategoryDisplayName = "Home & Garden"
        Assert.Equal("Home & Garden", result.CategoryDisplayName);

        // Assert - PDF Requirement: Price has 10% discount applied
        var expectedDiscountedPrice = Math.Round(originalPrice * 0.9m, 2); // 100 * 0.9 = 90.00
        Assert.Equal(expectedDiscountedPrice, result.Price);
        Assert.Equal(90.00m, result.Price);

        // Assert - PDF Requirement: ImageUrl is null (content filtering for Home category)
        Assert.Null(result.ImageUrl);

        // Additional assertions
        Assert.Equal("In Stock", result.AvailabilityStatus); // Stock = 15
        Assert.Equal("H", result.BrandInitials); // Single word brand
        
        // Assert - FormattedPrice (culture-independent)
        Assert.NotEmpty(result.FormattedPrice);
        Assert.True(
            result.FormattedPrice.Contains("90") && 
            (result.FormattedPrice.Contains(",00") || result.FormattedPrice.Contains(".00")),
            $"FormattedPrice '{result.FormattedPrice}' should contain formatted price 90");
    }

    /// <summary>
    /// Additional Test: Verify invalid SKU format throws validation exception
    /// </summary>
    [Fact]
    public async Task Handle_InvalidSKUFormat_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateProductProfileRequest(
            Name: "Digital Camera Test",
            Brand: "Test Brand",
            SKU: "invalid-sku-format", // Invalid: lowercase
            Category: ProductCategory.Electronics,
            Price: 99.99m,
            ReleaseDate: new DateTime(2024, 1, 15),
            ImageUrl: "https://example.com/test.jpg",
            StockQuantity: 10
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(request, CancellationToken.None)
        );

        Assert.NotNull(exception);
        Assert.Contains(exception.Errors, e => e.PropertyName == "SKU");
    }

    /// <summary>
    /// Additional Test: Verify Electronics without tech keywords fails validation
    /// </summary>
    [Fact]
    public async Task Handle_ElectronicsWithoutTechKeywords_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateProductProfileRequest(
            Name: "Red Box Product", // No tech keywords
            Brand: "Generic Brand",
            SKU: "ELEC-BOX-001",
            Category: ProductCategory.Electronics,
            Price: 199.99m,
            ReleaseDate: new DateTime(2024, 6, 15),
            ImageUrl: "https://example.com/box.jpg",
            StockQuantity: 10
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(request, CancellationToken.None)
        );

        Assert.NotNull(exception);
        Assert.Contains(exception.Errors, e =>
            e.ErrorMessage.Contains("technology-related keywords"));
    }

    /// <summary>
    /// Additional Test: Verify high-value product stock limit business rule
    /// </summary>
    [Fact]
    public async Task Handle_HighValueProductExceedsStockLimit_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateProductProfileRequest(
            Name: "Premium Smart Laptop",
            Brand: "Premium Tech",
            SKU: "PREMIUM-LAP-01",
            Category: ProductCategory.Electronics,
            Price: 1500.00m,
            ReleaseDate: new DateTime(2024, 1, 15),
            ImageUrl: "https://example.com/laptop.jpg",
            StockQuantity: 15 // > 10 for price > $500
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(request, CancellationToken.None)
        );

        Assert.NotNull(exception);
        Assert.Contains(exception.Errors, e =>
            e.ErrorMessage.Contains("business rule"));
    }

    /// <summary>
    /// Additional Test: Verify Home product price limit
    /// </summary>
    [Fact]
    public async Task Handle_HomeProductExceedsPriceLimit_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateProductProfileRequest(
            Name: "Luxury Garden Set",
            Brand: "HomeBrand",
            SKU: "HOME-LUX-01",
            Category: ProductCategory.Home,
            Price: 250.00m, // > $200
            ReleaseDate: new DateTime(2024, 6, 15),
            ImageUrl: "https://example.com/garden.jpg",
            StockQuantity: 5
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(request, CancellationToken.None)
        );

        Assert.NotNull(exception);
        Assert.Contains(exception.Errors, e =>
            e.ErrorMessage.Contains("$200"));
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}