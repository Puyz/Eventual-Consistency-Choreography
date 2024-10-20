using MassTransit;
using MongoDB.Driver;
using Shared;
using Shared.Events;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderCreatedEventConsumer(MongoDBService mongoDBService, ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint)
        {
            _mongoDBService = mongoDBService;
            _sendEndpointProvider = sendEndpointProvider;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            List<bool> stockResult = new();
            IMongoCollection<Models.Stock> collection = _mongoDBService.GetCollection<Models.Stock>();

            foreach (var orderItem in context.Message.OrderItems)
            {
                stockResult.Add(await (await collection.FindAsync(s => s.ProductId == orderItem.ProductId && s.Count >= orderItem.Count)).AnyAsync());
            }

            if (stockResult.TrueForAll(s => s.Equals(true)))
            {
                // stok güncellemesi
                foreach (var orderItem in context.Message.OrderItems)
                {
                    Models.Stock stock = await (await collection.FindAsync(s => s.ProductId == orderItem.ProductId)).FirstOrDefaultAsync();
                    stock.Count -= orderItem.Count;
                    await collection.FindOneAndReplaceAsync(s => s.ProductId == orderItem.ProductId, stock);
                }

                // payment'ı uyaracak event'in fırlatılması
                ISendEndpoint sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMQSettings.Payment_StockReservedEventQueue}"));
                StockReservedEvent stockReservedEvent = new()
                {
                    BuyerId = context.Message.BuyerId,
                    OrderId = context.Message.OrderId,
                    OrderItems = context.Message.OrderItems,
                    TotalPrice = context.Message.TotalPrice,
                };
                await sendEndpoint.Send(stockReservedEvent);
                
            }
            else
            {
                // Stock failed
                // orderı uyaracak event'in fırlatılması
                StockNotReservedEvent stockNotReservedEvent = new()
                {
                    BuyerId = context.Message.BuyerId,
                    OrderId = context.Message.OrderId,
                    Message = "Stok işlemi başarısız"
                };
                await _publishEndpoint.Publish(stockNotReservedEvent);
            }
        }
    }
}
