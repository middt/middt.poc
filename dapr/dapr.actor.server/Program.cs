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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapActorsHandlers();


// app.MapPost("/TransferMoney", async Task<IResult> ([FromBody] TransferRequest request) =>
app.MapPost("/TransferMoney", async Task<IResult> () =>
{
    var deleteActorID = new ActorId("ToAccountId");
    var deleteActor = ActorProxy.Create<IBankAccountActor>(deleteActorID, "BankAccountActor");
    // Deposit into the destination account
    await deleteActor.DepositAsync(10000000);
    var balance = await deleteActor.GetBalanceAsync();
    Parallel.For(1, 100, parallelOptions: new ParallelOptions() { MaxDegreeOfParallelism = 10 }, async index =>
    {
        var request = new TransferRequest() { Amount = 1, FromAccountId = "FromAccountId", ToAccountId = "ToAccountId" };

        var fromAccountActorId = new ActorId(request.FromAccountId);
        var toAccountActorId = new ActorId(request.ToAccountId);

        var fromAccountActor = ActorProxy.Create<IBankAccountActor>(fromAccountActorId, "BankAccountActor");
        var toAccountActor = ActorProxy.Create<IBankAccountActor>(toAccountActorId, "BankAccountActor");

        // Withdraw from the source account
        await fromAccountActor.WithdrawAsync(request.Amount);

        // Deposit into the destination account
        await toAccountActor.DepositAsync(request.Amount);
    });
    balance = await deleteActor.GetBalanceAsync();
    return Results.Ok("Transfer completed successfully");
})
.WithName("TransferMoney")
.WithOpenApi();

app.Run();
