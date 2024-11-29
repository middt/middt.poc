using Microsoft.AspNetCore.Http;
using elastic_minimal_api.Services;
using ElasticMinimalApi.Services;

namespace ElasticMinimalApi.Middleware;

public class ApmTraceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApmUrlPatternService _urlPatternService;
    private readonly IApmHeaderService _headerService;

    public ApmTraceMiddleware(
        RequestDelegate next,
        IApmUrlPatternService urlPatternService,
        IApmHeaderService headerService)
    {
        _next = next;
        _urlPatternService = urlPatternService;
        _headerService = headerService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _urlPatternService.SetTransactionName(context);
        _headerService.CaptureHeaders(context);

        await _next(context);
    }
}
