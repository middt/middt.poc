using Elastic.Apm;
using Microsoft.AspNetCore.Http;
using ElasticMinimalApi.Configuration;
using Microsoft.Extensions.Options;
using elastic_minimal_api.Services;

namespace ElasticMinimalApi.Services;

/// <summary>
/// Service to capture headers for Elastic APM
/// </summary>
public class ElasticApmHeaderService : IApmHeaderService
{
    private readonly HeaderMapping[] _headers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElasticApmHeaderService"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    public ElasticApmHeaderService(IOptions<CustomApmConfig> options)
    {
        _headers = options.Value.HeaderCapture.Headers;
    }

    /// <summary>
    /// Captures the headers.
    /// </summary>
    /// <param name="context">The context.</param>
    public void CaptureHeaders(HttpContext context)
    {
        var transaction = Agent.Tracer.CurrentTransaction;
        if (transaction == null) return;

        // Capture headers
        foreach (var header in _headers)
        {
            if (context.Request.Headers.TryGetValue(header.HeaderName, out var headerValue))
            {
                transaction.Context.Request.Headers[header.PropertyName] = headerValue.ToString();
            }
        }

        // Always capture content type and length if present
        if (context.Request.ContentType != null)
        {
            transaction.SetLabel("request.content_type", context.Request.ContentType);
        }

        if (context.Request.ContentLength.HasValue)
        {
            transaction.SetLabel("request.content_length", context.Request.ContentLength.Value);
        }

        // Capture response status code and content type
        transaction.SetLabel("response.status_code", context.Response.StatusCode);

        if (context.Response.ContentType != null)
        {
            transaction.SetLabel("response.content_type", context.Response.ContentType);
        }

        // Add trace ID for correlation
        var traceId = context.TraceIdentifier;
        transaction.SetLabel("trace.id", traceId);
        context.Response.Headers.Append("X-Trace-Id", traceId);
    }
}
