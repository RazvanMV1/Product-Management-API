using System.Diagnostics;

namespace Product_Management_API.Middleware;

public class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    private const string CorrelationHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers.ContainsKey(CorrelationHeader)
            ? context.Request.Headers[CorrelationHeader].ToString()
            : Guid.NewGuid().ToString("N").Substring(0, 8);
        
        context.Items[CorrelationHeader] = correlationId;
        context.Response.Headers[CorrelationHeader] = correlationId;
        
        using (logger.BeginScope(new Dictionary<string, object>
                   { ["CorrelationId"] = correlationId }))
        {
            logger.LogInformation("Incoming request: {Method} {Path} [CorrelationId: {CorrelationId}]",
                context.Request.Method, context.Request.Path, correlationId);

            var stopwatch = Stopwatch.StartNew();
            await next(context);
            stopwatch.Stop();

            logger.LogInformation("Request completed in {Duration} ms [CorrelationId: {CorrelationId}]",
                stopwatch.ElapsedMilliseconds, correlationId);
        }
    }
}

public static class CorrelationMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationMiddleware>();
    }
}