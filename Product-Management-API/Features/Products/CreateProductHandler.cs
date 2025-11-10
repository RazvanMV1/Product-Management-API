using Product_Management_API.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Product_Management_API.Common.Logging;
namespace Product_Management_API.Features.Products;

public class CreateProductHandler(
    ProductManagementContext context,
    ILogger<CreateProductHandler> logger,
    IMemoryCache cache,
    IMapper mapper)
{
    public async Task<ProductProfileDto> Handle(CreateProductProfileRequest request,
        CancellationToken cancellationToken)
    {
        // Generate a unique operation ID for tracing of 8 characters
        var OperationId = Guid.NewGuid().ToString("N").Substring(0, 8);
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = OperationId
        });
        // Start timing the operation with a stopwatch
        var stopwatch = Stopwatch.StartNew();
        var validationTimer = Stopwatch.StartNew();
        if (await context.Products.AnyAsync(p => p.SKU == request.SKU, cancellationToken))
        {
            validationTimer.Stop();
            stopwatch.Stop();
            var metricsError = new ProductCreationMetrics(
                OperationId,
                request.Name,
                request.SKU,
                request.Category,
                validationTimer.Elapsed,
                TimeSpan.Zero,
                stopwatch.Elapsed,
                false,
                "Duplicate SKU"
            );
            logger.LogProductCreationMetrics(metricsError);
            throw new InvalidOperationException($"A product with SKU {request.SKU} already exists.");
            
        }
        
        validationTimer.Stop();
        
        var dbSaveTimer = Stopwatch.StartNew();
        
        var product = mapper.Map<Product>(request);

        try
        {
            
        }
        catch (Exception e)
        {
                Console.WriteLine(e);
                throw;
        }
        
        await context.SaveChangesAsync(cancellationToken);
        
        dbSaveTimer.Stop();
        
        cache.Remove("all_products");
        
        logger.LogInformation("Created product {Name} ({Brand}) [{Category}] SKU={SKU}",
            product.Name, product.Brand, product.Category, product.SKU);
        
        stopwatch.Stop();
        
        var metrics = new ProductCreationMetrics(
            OperationId,
            product.Name,
            product.SKU,
            product.Category,
            validationTimer.Elapsed,
            dbSaveTimer.Elapsed,
            stopwatch.Elapsed,
            true
        );
        
        logger.LogProductCreationMetrics(metrics);
        
        return mapper.Map<ProductProfileDto>(product);    
        
    }
}