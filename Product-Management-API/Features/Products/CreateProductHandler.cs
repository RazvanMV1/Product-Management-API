using Product_Management_API.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Product_Management_API.Common.Logging;
using FluentValidation;

namespace Product_Management_API.Features.Products;

/// <summary>
/// Handles product creation with validation, logging, and performance tracking.
/// Orchestrates FluentValidation, AutoMapper transformations, structured logging with correlation IDs, and cache invalidation.
/// </summary>
public class CreateProductHandler
{
    private readonly ProductManagementContext _context;
    private readonly ILogger<CreateProductHandler> _logger;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateProductProfileRequest> _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductHandler"/> class.
    /// </summary>
    /// <param name="context">Database context for product persistence</param>
    /// <param name="logger">Structured logger for telemetry and diagnostics</param>
    /// <param name="cache">Memory cache for product collections</param>
    /// <param name="mapper">AutoMapper instance with product-specific mappings</param>
    /// <param name="validator">FluentValidation validator for product requests</param>
    public CreateProductHandler(
        ProductManagementContext context,
        ILogger<CreateProductHandler> logger,
        IMemoryCache cache,
        IMapper mapper,
        IValidator<CreateProductProfileRequest> validator)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _mapper = mapper;
        _validator = validator;
    }

    /// <summary>
    /// Creates a new product with comprehensive validation, logging, and performance tracking.
    /// </summary>
    /// <param name="request">Product creation request containing Name, Brand, SKU, Category, Price, ReleaseDate, ImageUrl, StockQuantity</param>
    /// <param name="cancellationToken">Cancellation token to observe while waiting for async operations</param>
    /// <returns>
    /// A <see cref="ProductProfileDto"/> with the created product details including calculated fields 
    /// (ProductAge, BrandInitials, AvailabilityStatus, FormattedPrice, CategoryDisplayName).
    /// </returns>
    /// <exception cref="ValidationException">
    /// Thrown when validation fails due to invalid SKU format, duplicate SKU/Name+Brand, business rule violations, 
    /// or category-specific validation failures (e.g., Electronics without tech keywords).
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when a duplicate SKU is detected in database check.</exception>
    /// <exception cref="DbUpdateException">Thrown when database save operation fails.</exception>
    /// <remarks>
    /// Performance metrics tracked: Validation duration (target &lt;50ms), Database save duration (target &lt;30ms), 
    /// Total duration (target &lt;100ms average). Logs 8 structured events (2001-2008) with correlation IDs for distributed tracing.
    /// </remarks>
    public async Task<ProductProfileDto> Handle(
        CreateProductProfileRequest request,
        CancellationToken cancellationToken)
    {
        // Generate a unique operation ID for tracing of 8 characters
        var operationId = Guid.NewGuid().ToString("N")[..8];
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationId"] = operationId
        });
        
        // Log operation start
        _logger.LogInformation(LogEvents.ProductCreationStarted, 
            "Starting product creation: Name={Name}, Brand={Brand}, Category={Category}, SKU={SKU}",
            request.Name, request.Brand, request.Category, request.SKU);
        
        // Start timing the operation
        var stopwatch = Stopwatch.StartNew();
        var validationTimer = Stopwatch.StartNew();
        
        // ========================================
        // FLUENT VALIDATION (Module 3)
        // ========================================
        _logger.LogInformation("Performing FluentValidation for product creation");
        
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            validationTimer.Stop();
            stopwatch.Stop();
            
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            
            _logger.LogWarning(LogEvents.ProductValidationFailed,
                "Product validation failed: {Errors}", errors);
            
            var metricsError = new ProductCreationMetrics(
                operationId,
                request.Name,
                request.SKU,
                request.Category,
                validationTimer.Elapsed,
                TimeSpan.Zero,
                stopwatch.Elapsed,
                false,
                $"Validation failed: {errors}"
            );
            
            _logger.LogProductCreationMetrics(metricsError);
            
            throw new ValidationException(validationResult.Errors);
        }
        
        _logger.LogInformation("FluentValidation passed successfully in {ElapsedMs}ms", 
            validationTimer.ElapsedMilliseconds);
        
        // ========================================
        // ADDITIONAL SKU UNIQUENESS CHECK
        // (Double-check for safety, already validated in FluentValidator)
        // ========================================
        _logger.LogInformation(LogEvents.SKUValidationPerformed, 
            "Performing additional SKU uniqueness check for SKU={SKU}", request.SKU);
        
        if (await _context.Products.AnyAsync(p => p.SKU == request.SKU, cancellationToken))
        {
            validationTimer.Stop();
            stopwatch.Stop();
            
            _logger.LogWarning(LogEvents.ProductValidationFailed,
                "Product validation failed: Duplicate SKU={SKU}", request.SKU);
            
            var metricsError = new ProductCreationMetrics(
                operationId,
                request.Name,
                request.SKU,
                request.Category,
                validationTimer.Elapsed,
                TimeSpan.Zero,
                stopwatch.Elapsed,
                false,
                "Duplicate SKU"
            );
            
            _logger.LogProductCreationMetrics(metricsError);
            
            throw new InvalidOperationException($"A product with SKU {request.SKU} already exists.");
        }
        
        validationTimer.Stop();
        
        // Stock validation log
        _logger.LogInformation(LogEvents.StockValidationPerformed,
            "Stock validation: StockQuantity={StockQuantity}", request.StockQuantity);
        
        // Map request to product entity
        var product = _mapper.Map<Product>(request);
        
        // Database operation with error handling
        var dbSaveTimer = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation(LogEvents.DatabaseOperationStarted,
                "Starting database save operation for product SKU={SKU}", request.SKU);
            
            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);
            
            dbSaveTimer.Stop();
            
            _logger.LogInformation(LogEvents.DatabaseOperationCompleted,
                "Database operation completed for ProductId={ProductId}, SKU={SKU}", 
                product.Id, product.SKU);
        }
        catch (Exception ex)
        {
            dbSaveTimer.Stop();
            stopwatch.Stop();
            
            _logger.LogError(LogEvents.ProductValidationFailed,
                ex, "Database error while creating product SKU={SKU}", request.SKU);
            
            var metricsError = new ProductCreationMetrics(
                operationId,
                request.Name,
                request.SKU,
                request.Category,
                validationTimer.Elapsed,
                dbSaveTimer.Elapsed,
                stopwatch.Elapsed,
                false,
                $"Database error: {ex.Message}"
            );
            
            _logger.LogProductCreationMetrics(metricsError);
            
            throw; // Re-throw for global error handler
        }
        
        // Cache invalidation
        _cache.Remove("all_products");
        _logger.LogInformation(LogEvents.CacheOperationPerformed,
            "Cache invalidated for key='all_products'");
        
        // Log successful creation
        _logger.LogInformation(LogEvents.ProductCreationCompleted,
            "Created product {Name} ({Brand}) [{Category}] SKU={SKU}",
            product.Name, product.Brand, product.Category, product.SKU);
        
        stopwatch.Stop();
        
        // Log success metrics
        var metrics = new ProductCreationMetrics(
            operationId,
            product.Name,
            product.SKU,
            product.Category,
            validationTimer.Elapsed,
            dbSaveTimer.Elapsed,
            stopwatch.Elapsed,
            true
        );
        
        _logger.LogProductCreationMetrics(metrics);
        
        // Return mapped DTO
        return _mapper.Map<ProductProfileDto>(product);
    }
}