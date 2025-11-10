namespace Product_Management_API.Common.Logging;

public static class LoggingExtensions
{
    public static void LogProductCreationMetrics(
        this ILogger logger,
        ProductCreationMetrics metrics)
    {
        logger.LogInformation(
            new EventId(LogEvents.ProductCreationCompleted, "ProductCreationCompleted"),
            "Product creation metrics: {Name} ({SKU}) [{Category}] | Validation={Validation}ms | DB={DB}ms | Total={Total}ms | Success={Success} | Error={Error}",
            metrics.ProductName,
            metrics.SKU,
            metrics.Category,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason ?? "None");
    }
}