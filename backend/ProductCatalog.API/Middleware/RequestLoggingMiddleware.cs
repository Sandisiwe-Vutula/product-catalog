using System.Diagnostics;

namespace ProductCatalog.API.Middleware
{
    /// <summary>
    /// Custom request/response logging middleware — built from scratch, not using
    /// built-in UseDeveloperExceptionPage or similar framework helpers.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Assign or propagate correlation ID
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                                ?? Guid.NewGuid().ToString("N")[..12];

            context.Response.Headers["X-Correlation-Id"] = correlationId;
            context.Items["CorrelationId"] = correlationId;

            var sw = Stopwatch.StartNew();

            try
            {
                await _next(context);
                sw.Stop();

                _logger.LogInformation(
                    "[{CorrelationId}] {Method} {Path} → {StatusCode} ({Elapsed}ms)",
                    correlationId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();

                _logger.LogError(ex,
                    "[{CorrelationId}] Unhandled exception on {Method} {Path} ({Elapsed}ms)",
                    correlationId,
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds);

                await WriteErrorResponseAsync(context, correlationId, ex);
            }
        }

        private static async Task WriteErrorResponseAsync(
            HttpContext context,
            string correlationId,
            Exception ex)
        {
            if (context.Response.HasStarted) return; // Can't modify headers once body has started

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "An unexpected error occurred.",
                correlationId,
                detail = ex.Message 
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
