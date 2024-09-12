
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var serviceName = "otel_testOpenObserve";
var serviceVersion = "1.0.0";
var openObserveEndpoint = "http://localhost:5080"; // Update this to your OpenObserve endpoint
var openObserveApiKey = "cm9vdEByb290LmNvbTp1aFJVNUVjZ1dLZDZzZUtk"; // Replace with your actual API key

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
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
                   // ... same configuration as above ...
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
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: builder.Environment.ApplicationName))
    .WithTracing(tracing =>
    {
        tracing.AddSource("exampleActivitySource.Name");
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddConsoleExporter();
        tracing.SetPropagators(new CompositeTextMapPropagator(
     new TextMapPropagator[]
     {
         new TraceContextPropagator(),
         new BaggagePropagator(),
         new B3Propagator(),
         new XRequestIdPropagator()
     }));
        tracing.AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://localhost:8081/ingest/otlp/v1/traces");
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            //opt.Headers = "X-Seq-ApiKey=abcde12345";
        });
    });
*/
Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
new TextMapPropagator[]
{
        new TraceContextPropagator(),
        new BaggagePropagator(),
        new B3Propagator(),
        new XRequestIdPropagator()
}));



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/execute", async Task<IResult> (ILogger<Program> logger) =>
{
    logger.LogInformation("This is a test info log from the /execute endpoint");
    logger.LogWarning("This is a test warning log from the /execute endpoint");
    logger.LogError("This is a test error log from the /execute endpoint");

    // ... existing code ...

    return Results.Ok("Logs generated successfully");
})
.WithName("execute")
.WithOpenApi();

app.Run();
