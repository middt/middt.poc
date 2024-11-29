using System.Text.RegularExpressions;

namespace distributed_cache.Configuration;

public class RedisSettings
{
    public string Configuration { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public int DefaultExpirationMinutes { get; set; } = 30;
    public List<EndpointCacheSettings> Endpoints { get; set; } = new();
}

public class EndpointCacheSettings
{
    private Regex? _compiledRegex;
    public string PathPattern { get; set; } = string.Empty;
    public int TimeToLiveMinutes { get; set; }

    public bool IsMatch(string path)
    {
        _compiledRegex ??= new Regex(PathPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        return _compiledRegex.IsMatch(path);
    }
}
