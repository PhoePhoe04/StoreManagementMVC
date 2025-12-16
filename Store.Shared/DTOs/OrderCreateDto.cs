using System.ComponentModel.DataAnnotations;

namespace Store.Shared.DTOs
{
    public class OrderCreateDTO
    {
        // Thông tin người mua
        [Required(ErrorMessage = "Tên không được để trống")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "SĐT không được để trống")]
        public string PhoneNumber { get; set; } = string.Empty;

        public int? PromoId { get; set; }

        // Danh sách hàng muốn mua
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
