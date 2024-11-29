using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.RegularExpressions;

namespace distributed_cache.Services;

public interface ICacheService
{
    Task<bool> InvalidateByKeyAsync(string cacheKey);
    Task<int> InvalidateByPatternAsync(string pattern);
    Task<IEnumerable<string>> GetAllKeysAsync(string pattern = "*");
}

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly ILogger<CacheService> _logger;
    private readonly string _instanceName;

    public CacheService(
        IDistributedCache cache, 
        IConnectionMultiplexer redisConnection,
        IConfiguration configuration,
        ILogger<CacheService> logger)
    {
        _cache = cache;
        _redisConnection = redisConnection;
        _logger = logger;
        _instanceName = configuration.GetSection("Redis:InstanceName").Value ?? "DistributedCache_";
    }

    public async Task<bool> InvalidateByKeyAsync(string cacheKey)
    {
        try
        {
            string fullKey = $"{_instanceName}{cacheKey}";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogInformation("Cache invalidated for key: {CacheKey}", cacheKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for key: {CacheKey}", cacheKey);
            return false;
        }
    }

    public async Task<int> InvalidateByPatternAsync(string pattern)
    {
        try
        {
            var keys = await GetAllKeysAsync(pattern);
            var count = 0;

            foreach (var key in keys)
            {
                // Remove the instance name prefix to get the original cache key
                var cacheKey = key.Substring(_instanceName.Length);
                await _cache.RemoveAsync(cacheKey);
                count++;
            }

            _logger.LogInformation("Invalidated {Count} cache entries matching pattern: {Pattern}", count, pattern);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for pattern: {Pattern}", pattern);
            return 0;
        }
    }

    public async Task<IEnumerable<string>> GetAllKeysAsync(string pattern = "*")
    {
        var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
        var keys = new List<string>();

        pattern = $"{_instanceName}{pattern}";
        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            keys.Add(key.ToString());
        }

        return keys;
    }
}
