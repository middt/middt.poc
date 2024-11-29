using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using distributed_cache.Configuration;

namespace distributed_cache.Middleware;

public class CacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheMiddleware> _logger;
    private readonly RedisSettings _redisSettings;

    public CacheMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        ILogger<CacheMiddleware> logger,
        RedisSettings redisSettings)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _redisSettings = redisSettings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = GetEndpointSettings(context.Request.Path);
        if (endpoint == null || !IsGetRequest(context.Request))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        var cacheKey = GenerateCacheKey(context.Request);

        try
        {
            var cachedResponse = await _cache.GetAsync(cacheKey);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Serving response from cache for key: {CacheKey}", cacheKey);
                context.Response.ContentType = "application/json";
                await context.Response.Body.WriteAsync(cachedResponse);
                return;
            }

            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            memoryStream.Position = 0;
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

            if (context.Response.StatusCode == 200)
            {
                _logger.LogInformation("Caching response for key: {CacheKey}", cacheKey);
                await _cache.SetAsync(cacheKey, 
                    Encoding.UTF8.GetBytes(responseBody),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(endpoint.TimeToLiveMinutes)
                    });
            }

            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private EndpointCacheSettings? GetEndpointSettings(PathString path)
    {
        return _redisSettings.Endpoints.FirstOrDefault(e => e.IsMatch(path.Value ?? string.Empty));
    }

    private static string GenerateCacheKey(HttpRequest request)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append($"{request.Path}");
        
        foreach (var (key, value) in request.Query.OrderBy(x => x.Key))
        {
            keyBuilder.Append($"|{key}-{value}");
        }
        
        return keyBuilder.ToString();
    }

    private static bool IsGetRequest(HttpRequest request)
    {
        return request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase);
    }
}
