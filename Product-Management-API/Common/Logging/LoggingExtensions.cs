namespace Product_Management_API.Common.Logging;

public static class LoggingExtensions
{
    public static void LogProductCreationMetrics(this ILogger logger, ProductCreationMetrics metrics)
    {
        logger.LogInformation(
            new EventId(LogEvents.ProductCreationCompleted, nameof(LogEvents.ProductCreationCompleted)),
            "Product Metrics | OperationId: {OperationId} | Name: {Name} | SKU: {SKU} | Category: {Category} | " +
            "Validation: {ValidationMs}ms | DB Save: {DbMs}ms | Total: {TotalMs}ms | Success: {Success} | Error: {Error}",
            metrics.OperationId,
            metrics.ProductName,
            metrics.SKU,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason ?? "None"
        );
    }
}