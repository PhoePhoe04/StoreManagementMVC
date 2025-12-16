namespace Store.Shared.DTOs
{
    public class ProductDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        // Đây là trường tổng hợp từ bảng Inventory
        public int Quantity { get; set; }

        // Thêm thông tin danh mục để hiển thị 
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
