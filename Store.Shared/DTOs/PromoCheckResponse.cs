
namespace Store.Shared.DTOs
{
    public class PromoCheckResponse
    {
        public bool Success { get; set; }
        public decimal DiscountAmount { get; set; }
        public int PromoId { get; set; }
    }
}
