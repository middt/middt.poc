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

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var serviceName = "otel_testOpenObserve";
var serviceVersion = "1.0.0";
var openObserveEndpoint = "http://localhost:5080";
var openObserveApiKey = "cm9vdEByb290LmNvbTp1aFJVNUVjZ1dLZDZzZUtk";


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
                   opt.Endpoint = new Uri($"{openObserveEndpoint}/api/default/v1/traces");
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
                   opt.Endpoint = new Uri($"{openObserveEndpoint}/api/default/v1/metrics");
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
               opt.Endpoint = new Uri($"{openObserveEndpoint}/api/default/v1/logs");
               opt.Headers = $"Authorization=Basic {openObserveApiKey}";
               opt.Protocol = OtlpExportProtocol.HttpProtobuf;
           });
});

/*
Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
new TextMapPropagator[]
{
        new TraceContextPropagator(),
        new BaggagePropagator(),
        new XRequestIdPropagator()
}));
*/


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/execute", async Task<IResult> (
    HttpContext httpContext, ILogger<Program> logger,
    IHttpClientFactory httpClientFactory) =>
{
    logger.LogInformation("This is a test info log from the /execute endpoint");
    logger.LogWarning("This is a test warning log from the /execute endpoint");
    logger.LogError("This is a test error log from the /execute endpoint");


    string xRequestId = httpContext.Request.Headers["x-request-id"];

    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("x-request-id", xRequestId);

    var response = await client.GetAsync("http://localhost:9082/private/anything/test");
    var content = await response.Content.ReadAsStringAsync();


    // client.DefaultRequestHeaders.Remove("x-request-id");
    response = await client.GetAsync("https://reqres.in/api/users?page=1");
    content = await response.Content.ReadAsStringAsync();


    response = await client.GetAsync("http://localhost:4400/otherapi");
    content = await response.Content.ReadAsStringAsync();

    response = await client.GetAsync("http://localhost:9082/private-local/otherapi");
    content = await response.Content.ReadAsStringAsync();


    return Results.Ok($"Logs generated successfully. API call response: {content}");
})
.WithName("execute")
.WithOpenApi();

app.MapGet("/otherapi", async Task<IResult> (HttpContext httpContext, ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    logger.LogInformation("This is a test info log from the /execute endpoint");
    logger.LogWarning("This is a test warning log from the /execute endpoint");
    logger.LogError("This is a test error log from the /execute endpoint");

    return Results.Ok($"Logs generated successfully.");
})
.WithName("otherapi")
.WithOpenApi();

app.Run();
