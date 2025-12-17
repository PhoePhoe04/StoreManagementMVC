
namespace Store.Shared.DTOs
{
    public class CreateOrderRequest
    {
        public string ReceiveName { get; set; } = "";
        public string ReceivePhone { get; set; } = "";
        public string ReceiveAddress { get; set; } = "";
        public string? Note { get; set; }
        public List<CartItemDTO> CartItems { get; set; } = new();
        public string? PromotionCode { get; set; }
    }
}
