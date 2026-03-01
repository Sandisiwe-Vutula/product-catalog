using ProductCatalog.API.Middleware;

namespace ProductCatalog.API.Extensions
{
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
            app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
