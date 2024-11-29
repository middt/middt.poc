using System.Diagnostics;
using dapr.actor.server.actors;
using Dapr.Actors;
using Dapr.Actors.Client;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddActors(options =>
        {
            options.Actors.RegisterActor<BankAccountActor>();
        });

var app = builder.Build();

#if DEBUG
    Debugger.Launch();
#endif

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// app.MapPost("/TransferMoney", async Task<IResult> ([FromBody] TransferRequest request) =>
app.MapPost("/TransferMoney", async Task<IResult> () =>
{
    var request = new TransferRequest() { Amount = 1, FromAccountId = "FromAccountId", ToAccountId = "ToAccountId" };

    var fromAccountActorId = new ActorId(request.FromAccountId);
    var fromAccountActor = ActorProxy.Create<IBankAccountActor>(fromAccountActorId, "BankAccountActor");
    await fromAccountActor.DepositAsync(10000000);

    // Parallel.For(1, 100, parallelOptions: new ParallelOptions() { MaxDegreeOfParallelism = 10 }, async index =>
    // {

    var toAccountActorId = new ActorId(request.ToAccountId);
    var toAccountActor = ActorProxy.Create<IBankAccountActor>(toAccountActorId, "BankAccountActor");

    // Withdraw from the source account
    await fromAccountActor.WithdrawAsync(request.Amount);

    // Deposit into the destination account
    await toAccountActor.DepositAsync(request.Amount);
    //});
    return Results.Ok("Transfer completed successfully");
})
.WithName("TransferMoney")
.WithOpenApi();

app.Run();
