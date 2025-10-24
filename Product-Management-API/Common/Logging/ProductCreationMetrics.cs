using Product_Management_API.Features.Products;

namespace Product_Management_API.Common.Logging;

public record ProductCreationMetrics
(
    string OperationId,
    string ProductName,
    string SKU,
    ProductCategory Category,
    TimeSpan ValidationDuration,
    TimeSpan DatabaseSaveDuration,
    TimeSpan TotalDuration,
    bool Success,
    string? ErrorReason = null
);