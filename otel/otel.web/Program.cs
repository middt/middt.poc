using Dapr.Actors;
using Dapr.Actors.Client;
using Microsoft.AspNetCore.Mvc;
using Dapr.Client;
using System.Text.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;

using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;
using System.Net.Http;
using System.Dynamic;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var serviceName = "bff";
var serviceVersion = "1.0.0";
var openObserveEndpoint = "http://172.23.0.2:5080/api/default";
var openObserveApiKey = "cm9vdEByb290LmNvbTpqd1IwaFo3ZFhWWUVMdG5l";


// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddConsoleExporter()
               .AddOtlpExporter(opt =>
               {
                   opt.Endpoint = new Uri($"{openObserveEndpoint}/v1/traces");
                   opt.Headers = $"Authorization=Basic {openObserveApiKey}";
                   opt.Protocol = OtlpExportProtocol.HttpProtobuf;

               });
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddConsoleExporter()
               .AddOtlpExporter(opt =>
               {
                   opt.Endpoint = new Uri($"{openObserveEndpoint}/v1/metrics");
                   opt.Headers = $"Authorization=Basic {openObserveApiKey}";
                   opt.Protocol = OtlpExportProtocol.HttpProtobuf;
               });
    });

// Configure logging
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion))
           .AddConsoleExporter()
           .AddOtlpExporter(opt =>
           {
               opt.Endpoint = new Uri($"{openObserveEndpoint}/v1/logs");
               opt.Headers = $"Authorization=Basic {openObserveApiKey}";
               opt.Protocol = OtlpExportProtocol.HttpProtobuf;
           });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/workflow", async Task<IResult> (
    HttpRequest request, ILogger<Program> logger,
    IHttpClientFactory httpClientFactory) =>
{
    logger.LogInformation("This is a test info log from the workflow endpoint");
    logger.LogWarning("This is a test warning log from the workflow endpoint");
    logger.LogError("This is a test error log from the workflow endpoint");

    // string xRequestId = httpContext.Request.Headers["x-request-id"];
    var client = httpClientFactory.CreateClient();
    // client.DefaultRequestHeaders.Add("x-request-id", xRequestId);
    // Copy headers from the incoming request to the HttpClient
    /*
     foreach (var header in request.Headers)
     {
         client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value.ToArray());
     }
 */

    var response = await client.GetAsync("http://localhost:9082/private/anything/test");
    var content = await response.Content.ReadAsStringAsync();


    // client.DefaultRequestHeaders.Remove("x-request-id");
    response = await client.GetAsync("https://reqres.in/api/users?page=1");
    content = await response.Content.ReadAsStringAsync();


    response = await client.GetAsync("http://localhost:4400/bffapi");
    content = await response.Content.ReadAsStringAsync();

    response = await client.GetAsync("http://localhost:9082/private-local/bffapi");
    content = await response.Content.ReadAsStringAsync();


    return Results.Ok($"Logs generated successfully. API call response: {content}");
})
.WithName("execute")
.WithOpenApi();

app.MapGet("/resource", async Task<IResult> (HttpRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation("This is a test info log from the resource endpoint");
    logger.LogWarning("This is a test warning log from the resource endpoint");
    logger.LogError("This is a test error log from the resource endpoint");

    return Results.Ok($"Logs generated successfully.");
})
.WithName("resource")
.WithOpenApi();

app.MapGet("/bffapi", async Task<IResult> (ILogger<Program> logger) =>
{
    logger.LogInformation("This is a test info log from the bffapi endpoint");
    logger.LogWarning("This is a test warning log from the bffapi endpoint");
    logger.LogError("This is a test error log from the bffapi endpoint");

    return Results.Ok($"Logs generated successfully.");
})
.WithName("bffapi")
.WithOpenApi();

app.MapPost("/introspection", async Task<IResult> (HttpRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation("This is a test info log from the introspection endpoint");
    logger.LogWarning("This is a test warning log from the introspection endpoint");
    logger.LogError("This is a test error log from the introspection endpoint");

    return Results.Json(new { active = true });
})
.WithName("introspection")
.WithOpenApi();

// Use the custom middleware
app.UseMiddleware<CustomHeaderLoggingMiddleware>();

app.Run();
