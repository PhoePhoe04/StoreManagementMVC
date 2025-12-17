namespace Store.Shared.DTOs
{
    public class PromotionCheckResult
    {
        public string PromoCode { get; set; } = "";
        public string DiscountType { get; set; } = "percent";
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
    }
}
