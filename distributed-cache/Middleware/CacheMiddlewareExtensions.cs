namespace distributed_cache.Middleware;

public static class CacheMiddlewareExtensions
{
    public static IApplicationBuilder UseDistributedCaching(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CacheMiddleware>();
    }
}
