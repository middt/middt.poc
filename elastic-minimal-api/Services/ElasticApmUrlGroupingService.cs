using System.Text.RegularExpressions;
using Elastic.Apm;
using Microsoft.AspNetCore.Http;
using ElasticMinimalApi.Configuration;
using Microsoft.Extensions.Options;
using elastic_minimal_api.Services;

namespace ElasticMinimalApi.Services;

public class ElasticApmUrlGroupingService : IApmUrlPatternService
{
    private readonly Configuration.UrlPattern[] _urlPatterns;

    public ElasticApmUrlGroupingService(IOptions<CustomApmConfig> options)
    {
        _urlPatterns = options.Value.UrlGroupingPatterns;
    }

    public void SetTransactionName(HttpContext context)
    {
        var transaction = Agent.Tracer.CurrentTransaction;
        if (transaction == null) return;

        var path = context.Request.Path.Value?.ToLower();
        var method = context.Request.Method;

        // Find matching pattern
        var matchedPattern = _urlPatterns.FirstOrDefault(p => 
            path != null && Regex.IsMatch(path, p.Pattern) && (p.Method == "*" || p.Method == method));

        transaction.Name = matchedPattern != null 
            ? $"{method} {matchedPattern.Template}"
            : $"{method} {path}";
    }
}
