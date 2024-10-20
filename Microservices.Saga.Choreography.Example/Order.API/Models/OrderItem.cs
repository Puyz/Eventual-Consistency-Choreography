namespace Order.API.Models
{
    public class OrderItem
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; }
        public int Count { get; set; }
        public decimal Price { get; set; }
    }
}
