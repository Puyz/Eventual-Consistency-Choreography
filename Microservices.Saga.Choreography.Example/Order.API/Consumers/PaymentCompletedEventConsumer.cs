using MassTransit;
using Order.API.Models.Contexts;
using Shared.Events;

namespace Order.API.Consumers
{
    public class PaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
    {
        private readonly OrderAPIDbContext _context;

        public PaymentCompletedEventConsumer(OrderAPIDbContext context)
        {
            _context = context;
        }

        public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
        {
            Models.Order order = await _context.Orders.FindAsync(context.Message.OrderId) ?? throw new NullReferenceException();

            order.OrderStatus = Enums.OrderStatus.Completed;
            await _context.SaveChangesAsync();
        }
    }
}
