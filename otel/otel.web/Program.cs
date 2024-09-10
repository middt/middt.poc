
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var serviceName = "apm_testElastic";
var serviceVersion = "1.0.0";


builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: builder.Environment.ApplicationName))
    .WithTracing(tracing =>
    {
        tracing.AddSource("exampleActivitySource.Name");
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddConsoleExporter();
        tracing.AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://localhost:8200");
            opt.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            //opt.Headers = "X-Seq-ApiKey=abcde12345";
        });
    });

Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(
new TextMapPropagator[]
{
        new TraceContextPropagator(),
        // new BaggagePropagator(),
        // new B3Propagator(),
        new XRequestIdPropagator()
}));





/*
builder.Services.OpenTelemetry(traceProviderBuilder =>
        {
            traceProviderBuilder
            .AddSource(serviceName)
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddEnvironmentVariableDetector())
            .AddAspNetCoreInstrumentation(option => { option.RecordException = true; })
            .AddHttpClientInstrumentation(option => { option.RecordException = true; })
            .AddSqlClientInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(configs =>
            {
                configs.Endpoint = new Uri("http://localhost:8200");
                configs.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            })
            ;
        });
*/


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/execute", async Task<IResult> () =>
{

    // Create an instance of HttpClient
    using (HttpClient client = new HttpClient())
    {
        // Set the base address (optional)
        client.BaseAddress = new Uri("https://api.example.com/");

        // Set a default request header (optional)
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        // Send a GET request
        HttpResponseMessage response = await client.GetAsync("endpoint");

        if (response.IsSuccessStatusCode)
        {
            // Read the response content as a string
            string content = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Response content:");
            Console.WriteLine(content);
        }
        else
        {
            Console.WriteLine($"Request failed with status code: {response.StatusCode}");
        }
    }

    return Results.Ok();

})
.WithName("execute")
.WithOpenApi();


app.Run();
