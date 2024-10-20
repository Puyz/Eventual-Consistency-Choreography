using MassTransit;
using Order.API.Models.Contexts;
using Shared.Events;

namespace Order.API.Consumers
{
    public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
    {
        private readonly OrderAPIDbContext _context;

        public PaymentFailedEventConsumer(OrderAPIDbContext context)
        {
            _context = context;
        }
        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            Models.Order order = await _context.Orders.FindAsync(context.Message.OrderId) ?? throw new NullReferenceException();

            order.OrderStatus = Enums.OrderStatus.Fail;
            await _context.SaveChangesAsync();
        }
    }
}
