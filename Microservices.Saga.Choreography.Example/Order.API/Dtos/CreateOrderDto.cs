namespace Order.API.Dtos
{
    public class CreateOrderDto
    {
        public string BuyerId { get; set; }
        public List<CreateOrderItemDto> OrderItems { get; set; }
    }
    
}
