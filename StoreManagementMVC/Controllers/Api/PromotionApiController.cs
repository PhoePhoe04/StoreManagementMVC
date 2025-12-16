using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementMVC.Data;

namespace StoreManagementMVC.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PromotionApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PromotionApi/Check?code=SUMMER&total=100000
        [HttpGet("Check")]
        public async Task<IActionResult> CheckCode(string code, decimal total)
        {
            var promo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromoCode == code && p.Status == "active");

            if (promo == null)
                return BadRequest("Mã không tồn tại hoặc đã hết hạn.");

            if (DateTime.Now < promo.StartDate || DateTime.Now > promo.EndDate)
                return BadRequest("Mã chưa bắt đầu hoặc đã hết hạn.");

            if (promo.UsageLimit > 0 && promo.UsedCount >= promo.UsageLimit)
                return BadRequest("Mã đã hết lượt sử dụng.");

            if (total < promo.MinOrderAmount)
                return BadRequest($"Đơn hàng phải từ {promo.MinOrderAmount:N0}đ.");

            // Tính tiền giảm để trả về cho Client hiển thị
            decimal discountAmt = promo.DiscountType == "percent"
                ? total * (promo.DiscountValue / 100)
                : promo.DiscountValue;

            if (discountAmt > total) discountAmt = total;

            return Ok(new
            {
                Success = true,
                DiscountAmount = discountAmt,
                PromoId = promo.PromoId
            });
        }
    }
}
