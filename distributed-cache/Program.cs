using distributed_cache.Configuration;
using distributed_cache.Middleware;
using distributed_cache.Services;
using StackExchange.Redis;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Redis
var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>()
    ?? throw new InvalidOperationException("Redis settings are not configured");
builder.Services.AddSingleton(redisSettings);

// Configure Redis Connection
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisSettings.Configuration));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisSettings.Configuration;
    options.InstanceName = redisSettings.InstanceName;
});

// Register cache service
builder.Services.AddScoped<ICacheService, CacheService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add our custom caching middleware
app.UseDistributedCaching();

// Cache management endpoints
app.MapDelete("/cache/{key}", async (string key, ICacheService cacheService) =>
{
    var result = await cacheService.InvalidateByKeyAsync(key);
    return result ? Results.Ok($"Cache invalidated for key: {key}") 
                 : Results.BadRequest($"Failed to invalidate cache for key: {key}");
})
.WithName("InvalidateCache")
.WithOpenApi();

app.MapDelete("/cache/pattern/{pattern}", async (string pattern, ICacheService cacheService) =>
{
    var count = await cacheService.InvalidateByPatternAsync(pattern);
    return count > 0 
        ? Results.Ok($"Invalidated {count} cache entries matching pattern: {pattern}")
        : Results.BadRequest($"No cache entries found matching pattern: {pattern}");
})
.WithName("InvalidateCacheByPattern")
.WithOpenApi();

app.MapGet("/cache/keys/{pattern?}", async (string? pattern, ICacheService cacheService) =>
{
    var keys = await cacheService.GetAllKeysAsync(pattern ?? "*");
    return Results.Ok(keys);
})
.WithName("GetCacheKeys")
.WithOpenApi();

// Sample endpoints
app.MapGet("/weatherforecast", async (ILogger<Program> logger) =>
{
    string[] summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

    logger.LogInformation("Generating weather forecast");
    
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

// Sample products endpoints
app.MapGet("/api/products", () =>
{
    var products = new[]
    {
        new { Id = 1, Name = "Product 1", Price = 10.99m },
        new { Id = 2, Name = "Product 2", Price = 20.99m },
        new { Id = 3, Name = "Product 3", Price = 30.99m }
    };
    
    return products;
})
.WithName("GetProducts")
.WithOpenApi();

app.MapGet("/api/products/{id}", (int id) =>
{
    var product = new { Id = id, Name = $"Product {id}", Price = 10.99m * id };
    return product;
})
.WithName("GetProductById")
.WithOpenApi();

// Sample categories endpoints
app.MapGet("/api/categories", () =>
{
    var categories = new[]
    {
        new { Id = 1, Name = "Electronics" },
        new { Id = 2, Name = "Books" },
        new { Id = 3, Name = "Clothing" }
    };
    
    return categories;
})
.WithName("GetCategories")
.WithOpenApi();

app.MapGet("/api/categories/{id}/products", (int id) =>
{
    var products = new[]
    {
        new { Id = 1, CategoryId = id, Name = $"Product 1 in Category {id}" },
        new { Id = 2, CategoryId = id, Name = $"Product 2 in Category {id}" }
    };
    
    return products;
})
.WithName("GetCategoryProducts")
.WithOpenApi();

app.Run();
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

