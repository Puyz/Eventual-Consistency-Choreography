using MassTransit;
using Order.API.Models.Contexts;
using Shared.Events;

namespace Order.API.Consumers
{
    public class StockNotReservedEventConsumer : IConsumer<StockNotReservedEvent>
    {
        private readonly OrderAPIDbContext _context;

        public StockNotReservedEventConsumer(OrderAPIDbContext context)
        {
            _context = context;
        }

        public async Task Consume(ConsumeContext<StockNotReservedEvent> context)
        {
            Models.Order order = await _context.Orders.FindAsync(context.Message.OrderId) ?? throw new NullReferenceException();

            order.OrderStatus = Enums.OrderStatus.Fail;
            await _context.SaveChangesAsync();
        }
    }
}
