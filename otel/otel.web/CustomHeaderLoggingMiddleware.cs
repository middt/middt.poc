using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

public class CustomHeaderLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _optionalHeaders = {         "X-Request-Id",
        "Header1",
        "X-Demo-Id",
        "user_reference" }; // Add your desired headers here
    public CustomHeaderLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get the current activity
        var currentActivity = Activity.Current;

        if (currentActivity != null)
        {
            // Log specified optional headers
            foreach (var headerKey in _optionalHeaders)
            {
                if (context.Request.Headers.TryGetValue(headerKey, out var headerValue))
                {
                    currentActivity.SetTag($"http.request.header.{headerKey}", headerValue.ToString());
                }
            }
        }

        await _next(context);
    }
}