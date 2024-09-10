
using Dapr.Actors;
using Dapr.Actors.Client;
using Microsoft.AspNetCore.Mvc;
using Dapr.Client;
using System.Text.Json;


const string envVariableBindingName = "BINDING_NAME";
const string envVariableTopicName = "TOPIC_NAME";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient();

builder.Services.AddActors(options =>
        {
            options.Actors.RegisterActor<ExecutorActor>();
        });
builder.Services.AddSingleton<IMessageDeserializationService, MessageDeserializationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapActorsHandlers();


app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapPost("/subscribe", async Task<IResult> (HttpContext context, [FromServices] IMessageDeserializationService deserializationService) =>
{
    var message = await deserializationService.DeserializeMessageAsync(context.Request.Body);

    return Results.Ok(message);

})
.WithTopic(Environment.GetEnvironmentVariable(envVariableBindingName), Environment.GetEnvironmentVariable(envVariableTopicName))
.WithName("subscribe")
.WithOpenApi();

app.MapPost("/publish", async Task<IResult> ([FromServices] DaprClient client) =>
{
    await client.PublishEventAsync<string>(Environment.GetEnvironmentVariable(envVariableBindingName), Environment.GetEnvironmentVariable(envVariableTopicName), "Hello World !!!");
    return Results.Ok();
})
.WithName("publish")
.WithOpenApi();


app.Run();
