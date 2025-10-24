using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Product_Management_API.Features.Products;
using Product_Management_API.Persistence;
using Product_Management_API.Common.Logging;

namespace Product_Management_API.Features.Products
{
    public class CreateProductHandler(
        ProductManagementContext context,
        ILogger<CreateProductHandler> logger,
        IMemoryCache cache)
    {
        public async Task<Product> Handle(CreateProductProfileRequest request)
        {
            var operationId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var totalWatch = Stopwatch.StartNew();
            var validationWatch = new Stopwatch();
            var dbWatch = new Stopwatch();

            using (logger.BeginScope(new { OperationId = operationId, request.SKU }))
            {
                try
                {
                    logger.LogInformation(
                        new EventId(LogEvents.ProductCreationStarted, nameof(LogEvents.ProductCreationStarted)),
                        "Starting product creation for {Name}, {Brand}, {SKU}, {Category}",
                        request.Name, request.Brand, request.SKU, request.Category);
                    
                    logger.LogInformation(
                        new EventId(LogEvents.SKUValidationPerformed, nameof(LogEvents.SKUValidationPerformed)),
                        "Validating SKU uniqueness for {SKU}", request.SKU);

                    validationWatch.Start();

                    bool skuExists = await context.Products.AnyAsync(p => p.SKU == request.SKU);
                    if (skuExists)
                    {
                        logger.LogWarning(
                            new EventId(LogEvents.ProductValidationFailed, nameof(LogEvents.ProductValidationFailed)),
                            "Validation failed: SKU {SKU} already exists", request.SKU);
                        throw new InvalidOperationException($"Product with SKU {request.SKU} already exists.");
                    }

                    validationWatch.Stop();
                    
                    logger.LogInformation(
                        new EventId(LogEvents.StockValidationPerformed, nameof(LogEvents.StockValidationPerformed)),
                        "Stock validation performed for {Name} (Quantity: {Qty})", request.Name, request.StockQuantity);
                    
                    var product = new Product(
                        Guid.NewGuid(),
                        request.Name,
                        request.Brand,
                        request.SKU,
                        request.Category,
                        request.Price,
                        request.ReleaseDate,
                        request.ImageUrl,
                        request.StockQuantity > 0,
                        request.StockQuantity
                    );
                    
                    logger.LogInformation(
                        new EventId(LogEvents.DatabaseOperationStarted, nameof(LogEvents.DatabaseOperationStarted)),
                        "Starting database save for product {Name}", request.Name);

                    dbWatch.Start();

                    context.Products.Add(product);
                    await context.SaveChangesAsync();

                    dbWatch.Stop();

                    logger.LogInformation(
                        new EventId(LogEvents.DatabaseOperationCompleted, nameof(LogEvents.DatabaseOperationCompleted)),
                        "Database save completed for {Name} (Id: {Id}) in {Duration}ms",
                        product.Name, product.Id, dbWatch.Elapsed.TotalMilliseconds);
                    
                    logger.LogInformation(
                        new EventId(LogEvents.CacheOperationPerformed, nameof(LogEvents.CacheOperationPerformed)),
                        "Cache invalidated for key 'all_products'");
                    cache.Remove("all_products");

                    totalWatch.Stop();
                    
                    var metrics = new ProductCreationMetrics(
                        OperationId: operationId,
                        ProductName: product.Name,
                        SKU: product.SKU,
                        Category: product.Category,
                        ValidationDuration: validationWatch.Elapsed,
                        DatabaseSaveDuration: dbWatch.Elapsed,
                        TotalDuration: totalWatch.Elapsed,
                        Success: true
                    );

                    logger.LogProductCreationMetrics(metrics);

                    return product;
                }
                catch (Exception ex)
                {
                    totalWatch.Stop();

                    var errorMetrics = new ProductCreationMetrics(
                        OperationId: operationId,
                        ProductName: request.Name,
                        SKU: request.SKU,
                        Category: request.Category,
                        ValidationDuration: validationWatch.Elapsed,
                        DatabaseSaveDuration: dbWatch.Elapsed,
                        TotalDuration: totalWatch.Elapsed,
                        Success: false,
                        ErrorReason: ex.Message
                    );

                    logger.LogError(
                        new EventId(LogEvents.ProductValidationFailed, nameof(LogEvents.ProductValidationFailed)),
                        ex,
                        "Error creating product {Name} ({SKU}): {Message}",
                        request.Name, request.SKU, ex.Message);

                    logger.LogProductCreationMetrics(errorMetrics);
                    throw;
                }
            }
        }
    }
}
