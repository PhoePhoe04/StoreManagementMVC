namespace StoreManagementMVC.DTO
{
    public class OrderRequestDTO
    {
        public int UserId { get; set; } // ID user đặt hàng (lấy từ Auth sau này)
        public int CustomerId { get; set; } // ID khách hàng (nếu có)
        public string PaymentMethod { get; set; } = "cash";
        public string? PromoCode { get; set; }
        public List<CartItemDTO> Items { get; set; } = new();
    }
}
