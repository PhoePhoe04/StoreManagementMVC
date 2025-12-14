using StoreManagementMVC.Models;

namespace StoreManagementMVC.Models
{
    public class OrderCreationVM
    {
        public int CustomerId { get; set; }
        public List<OrderItemVM> Items { get; set; } = new();
        public string? PromoCode { get; set; }
        public string PaymentMethod { get; set; } = "cash";
    }

    public class OrderItemVM
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } // Giá bán tại thời điểm tạo đơn
    }
}
