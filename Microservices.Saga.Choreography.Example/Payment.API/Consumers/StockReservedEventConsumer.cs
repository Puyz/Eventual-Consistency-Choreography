using MassTransit;
using Shared.Events;

namespace Payment.API.Consumers
{
    public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public StockReservedEventConsumer(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<StockReservedEvent> context)
        {
            bool isCompleted = false;
            if (isCompleted)
            {
                // ödeme başarılı
                PaymentCompletedEvent paymentCompletedEvent = new()
                {
                    OrderId = context.Message.OrderId
                };
                await _publishEndpoint.Publish(paymentCompletedEvent);
                await Console.Out.WriteLineAsync("Ödeme başarılı.");
            }
            else
            {
                // ödeme başarısız
                PaymentFailedEvent paymentFailedEvent = new()
                {
                    OrderId = context.Message.OrderId,
                    OrderItems = context.Message.OrderItems,
                    Message = "Bakiye yetersiz"
                };
                await _publishEndpoint.Publish(paymentFailedEvent);
                await Console.Out.WriteLineAsync("Ödeme başarısız.");
            }
        }
    }
}
