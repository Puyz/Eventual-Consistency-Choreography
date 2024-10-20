using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Consumers;
using Order.API.Dtos;
using Order.API.Models;
using Order.API.Models.Contexts;
using Shared;
using Shared.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<PaymentCompletedEventConsumer>();
    configurator.AddConsumer<PaymentFailedEventConsumer>();
    configurator.AddConsumer<StockNotReservedEventConsumer>();

    configurator.UsingRabbitMq((context, _configure) =>
    {
        _configure.Host(builder.Configuration.GetConnectionString("RabbitMQ"));

        _configure.ReceiveEndpoint(RabbitMQSettings.Order_PaymentCompletedEventQueue, e => e.ConfigureConsumer<PaymentCompletedEventConsumer>(context));
        _configure.ReceiveEndpoint(RabbitMQSettings.Order_PaymentFailedEventQueue, e => e.ConfigureConsumer<PaymentFailedEventConsumer>(context));
        _configure.ReceiveEndpoint(RabbitMQSettings.Order_StockNotReservedEventQueue, e => e.ConfigureConsumer<StockNotReservedEventConsumer>(context));
    });
});

builder.Services.AddDbContext<OrderAPIDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SQLServer"));
});


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/create-order", async (CreateOrderDto createOrder, OrderAPIDbContext context, IPublishEndpoint publishEndpoint) =>
{
    Order.API.Models.Order order = new()
    {
        BuyerId = Guid.TryParse(createOrder.BuyerId, out Guid _buyerId) ? _buyerId : throw new Exception(),
        OrderItems = createOrder.OrderItems.Select(oi => new OrderItem()
        {
            Count = oi.Count,
            Price = oi.Price,
            ProductId = oi.ProductId,
        }).ToList(),
        TotalPrice = createOrder.OrderItems.Sum(oi => oi.Count * oi.Price),
        CreatedDate = DateTime.Now,
        OrderStatus = Order.API.Enums.OrderStatus.Suspend
    };

    await context.Orders.AddAsync(order);
    await context.SaveChangesAsync();

    OrderCreatedEvent orderCreatedEvent = new()
    {
        OrderId = order.Id,
        BuyerId = order.BuyerId,
        TotalPrice = order.TotalPrice,
        OrderItems = order.OrderItems.Select(oi => new Shared.Messages.OrderItemMessage()
        {
            Count = oi.Count,
            Price = oi.Price,
            ProductId = oi.ProductId,
        }).ToList()
    };
    await publishEndpoint.Publish(orderCreatedEvent);

});


app.Run();
