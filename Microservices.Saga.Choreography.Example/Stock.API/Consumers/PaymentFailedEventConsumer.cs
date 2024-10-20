using MassTransit;
using MongoDB.Driver;
using Shared.Events;
using Stock.API.Services;

namespace Stock.API.Consumers
{
    public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
    {
        private readonly MongoDBService _mongoDBService;

        public PaymentFailedEventConsumer(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            var stocks = _mongoDBService.GetCollection<Models.Stock>();
            foreach (var orderItem in context.Message.OrderItems)
            {
                var stock = await (await stocks.FindAsync(s => s.ProductId == orderItem.ProductId)).FirstOrDefaultAsync();
                if (stock != null)
                {
                    stock.Count += orderItem.Count;
                    await stocks.FindOneAndReplaceAsync(s => s.ProductId == orderItem.ProductId, stock);
                }
            }
        }
    }
}
