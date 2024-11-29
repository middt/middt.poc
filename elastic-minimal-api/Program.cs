using Elastic.Apm.AspNetCore;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Headers;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.OpenApi.Models;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm;
using ElasticMinimalApi.Services;
using ElasticMinimalApi.Configuration;
using System.Text.RegularExpressions;
using elastic_minimal_api.Services;
using ElasticMinimalApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Custom APM settings
builder.Services.Configure<CustomApmConfig>(
    builder.Configuration.GetSection("CustomApm"));

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IApmHeaderService, ElasticApmHeaderService>();
builder.Services.AddSingleton<IApmUrlPatternService, ElasticApmUrlGroupingService>();

var app = builder.Build();

// Add Elastic APM middleware with URL grouping
app.UseAllElasticApm(builder.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware to add trace headers and capture APM data
app.UseApmTrace();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/", () => "Hello World!")
    .WithName("GetRoot")
    .WithOpenApi();

app.MapGet("/items/{id}", (string id) =>
{
    return Results.Ok(new { Id = id, Name = $"Item {id}" });
})
.WithName("GetItem")
.WithOpenApi();

app.MapPost("/items", (Item item) =>
{
    return Results.Created($"/items/{item.Id}", item);
})
.WithName("CreateItem")
.WithOpenApi();

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/test-apm", async (HttpContext context) =>
{
    // Create a custom span
    var transaction = Elastic.Apm.Agent.Tracer.CurrentTransaction;
    var span = transaction?.StartSpan("test-operation", "test");

    try
    {
        // Simulate some work
        await Task.Delay(100);

        // Add custom labels
        transaction?.SetLabel("test_label", "test_value");

        return Results.Ok(new { message = "APM test successful", traceId = transaction?.TraceId });
    }
    finally
    {
        span?.End();
    }
})
.WithName("TestApm")
.WithOpenApi();

app.Run();

// Define Item record
public record Item(string Id, string Name);

// Define WeatherForecast record
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Define URL pattern configuration class
public class UrlPattern
{
    public string Pattern { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Method { get; set; } = "*";  // "*" means match any method
}