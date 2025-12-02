using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagement.Models
{
    [Table("promotions")]
    public class Promotion
    {
        [Key]
        [Column("promo_id")]
        public int PromoId { get; set; }

        [Column("promo_code")]
        [Required]
        public string PromoCode { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("discount_type")]
        public string DiscountType { get; set; } = "percent"; // percent, fixed

        [Column("discount_value")]
        public decimal DiscountValue { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("min_order_amount")]
        public decimal MinOrderAmount { get; set; }

        [Column("usage_limit")]
        public int UsageLimit { get; set; }

        [Column("status")]
        public string Status { get; set; } = "active";
    }
}
