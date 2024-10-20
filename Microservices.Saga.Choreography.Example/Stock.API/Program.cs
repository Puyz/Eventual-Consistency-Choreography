using MassTransit;
using MongoDB.Driver;
using Shared;
using Stock.API.Consumers;
using Stock.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<OrderCreatedEventConsumer>();
    configurator.AddConsumer<PaymentFailedEventConsumer>();

    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration.GetConnectionString("RabbitMQ"));

        _configure.ReceiveEndpoint(RabbitMQSettings.Stock_OrderCreatedEventQueue, e => e.ConfigureConsumer<OrderCreatedEventConsumer>(context));
        _configure.ReceiveEndpoint(RabbitMQSettings.Stock_PaymentFailedEventQueue, e => e.ConfigureConsumer<PaymentFailedEventConsumer>(context));
    });
});

builder.Services.AddSingleton<MongoDBService>();

var app = builder.Build();

using IServiceScope scope = app.Services.CreateScope();
MongoDBService mongoDBService = scope.ServiceProvider.GetService<MongoDBService>()!;
var stockCollection = mongoDBService.GetCollection<Stock.API.Models.Stock>();

if (!stockCollection.FindSync(session => true).Any())
{
    await stockCollection.InsertManyAsync(new List<Stock.API.Models.Stock>()
    {
         new() { ProductId = Guid.NewGuid().ToString(), Count = 150 },
         new() { ProductId = Guid.NewGuid().ToString(), Count = 100 },
         new() { ProductId = Guid.NewGuid().ToString(), Count = 50 },
         new() { ProductId = Guid.NewGuid().ToString(), Count = 25 },
         new() { ProductId = Guid.NewGuid().ToString(), Count = 5 },
    });
}

app.Run();
