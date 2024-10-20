namespace Order.API.Dtos
{
    public class CreateOrderItemDto
    {
        public string ProductId { get; set; }
        public int Count { get; set; }
        public decimal Price { get; set; }
    }
}