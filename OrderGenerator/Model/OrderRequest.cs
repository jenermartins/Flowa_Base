namespace OrderGenerator.Models
{
    public class OrderRequest
    {
        public required string Symbol { get; set; }
        public required string Side { get; set; }
        public required int Quantity { get; set; }
        public required decimal Price { get; set; }
    }

    public class OrderResponse
    {
        public required string OrderId { get; set; }
        public required bool Accepted { get; set; }
        public required string? Message { get; set; }
        public decimal CurrentExposure { get; set; }
    }
}