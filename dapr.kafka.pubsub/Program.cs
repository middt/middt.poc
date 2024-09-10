
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

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: builder.Environment.ApplicationName))
    .WithTracing(tracing =>
    {
        tracing.AddSource("exampleActivitySource.Name");
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddConsoleExporter();
        /* tracing.SetPropagators(new CompositeTextMapPropagator(
     new TextMapPropagator[]
     {
         new TraceContextPropagator(),
         new BaggagePropagator(),
         new B3Propagator(),
         new XRequestIdPropagator()
     }));*/
        tracing.AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://localhost:8081/ingest/otlp/v1/traces");
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
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
builder.Services.AddOpenTelemetryTracing(builder =>
   {
       builder
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .SetSampler(new AlwaysOnSampler())
           .AddJaegerExporter() // or any other exporter
           .AddProcessor(new BatchActivityExportProcessor())
           .AddSource("YourApplicationSource")
           .SetPropagators(new CompositeTextMapPropagator(new XRequestIdPropagator(), new TraceContextPropagator()));
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
app.MapActorsHandlers();


app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapPost("/subscribe", async Task<IResult> (HttpContext context, [FromServices] IMessageDeserializationService deserializationService) =>
{
    var message = await deserializationService.DeserializeMessageAsync(context.Request.Body);

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

    return Results.Ok(message);

})
.WithTopic(Environment.GetEnvironmentVariable(envVariableBindingName), Environment.GetEnvironmentVariable(envVariableTopicName))
.WithName("subscribe")
.WithOpenApi();

app.MapPost("/publish", async Task<IResult> ([FromServices] DaprClient client) =>
{
    await client.PublishEventAsync<string>(
Environment.GetEnvironmentVariable(envVariableBindingName), Environment.GetEnvironmentVariable(envVariableTopicName),
         "Hello World !!!");
    return Results.Ok();
})
.WithName("publish")
.WithOpenApi();


app.Run();
