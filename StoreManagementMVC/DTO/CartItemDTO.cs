namespace StoreManagementMVC.DTO
{
    public class CartItemDTO
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } // Giá tại thời điểm đặt
    }
}
