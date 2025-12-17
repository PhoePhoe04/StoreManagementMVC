namespace Store.Shared.DTOs
{
    public class CartItemDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1; // Số lượng khách mua
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
    }
}
