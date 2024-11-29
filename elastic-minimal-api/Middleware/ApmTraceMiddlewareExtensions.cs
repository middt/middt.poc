using Microsoft.AspNetCore.Builder;

namespace ElasticMinimalApi.Middleware;

public static class ApmTraceMiddlewareExtensions
{
    public static IApplicationBuilder UseApmTrace(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApmTraceMiddleware>();
    }
}
