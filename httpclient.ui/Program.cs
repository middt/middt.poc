using httpclient.ui;
using httpclient.ui.Handler;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddTransient<LoggingDelegatingHandler>();
builder.Services.AddHttpClient<PostService>(httpClient =>
{
    httpClient.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
}).AddHttpMessageHandler<LoggingDelegatingHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/make-http-call", async (int userId,PostService postService) =>
    {
        var content = await postService.GetByUserIdAsync(userId);
        return Results.Ok(content);
    })
    .WithName("make-http-call")
    .WithOpenApi();

app.Run();
