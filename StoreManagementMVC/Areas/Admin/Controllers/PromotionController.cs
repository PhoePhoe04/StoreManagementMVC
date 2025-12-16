using Microsoft.AspNetCore.Mvc;
using Store.Shared.Entities;
using StoreManagementMVC.Services;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class PromotionController : Controller
    {
        private readonly PromotionService _service;

        public PromotionController(PromotionService service) => _service = service;

        public IActionResult Index(int p = 1, string search = "")
        {
            var result = _service.GetPromotionsPaging(p, 10, search);
            ViewBag.CurrentPage = p;
            ViewBag.TotalPages = (int)Math.Ceiling((double)result.totalCount / 10);
            ViewBag.Search = search;
            return View(result.promotions);
        }

        public IActionResult Upsert(int? id)
        {
            if (id == null)
            {
                // Giá trị mặc định khi tạo mới
                return PartialView("_Upsert", new Promotion
                {
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7), // Mặc định 7 ngày
                    Status = "active",
                    DiscountType = "percent"
                });
            }

            var promo = _service.GetPromotionById(id.Value);
            if (promo == null) return NotFound();

            return PartialView("_Upsert", promo);
        }

        [HttpPost]
        public IActionResult Upsert(Promotion promo)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            try
            {
                // Validate Logic ngày tháng
                if (promo.EndDate < promo.StartDate)
                    return Json(new { success = false, message = "Ngày kết thúc phải lớn hơn ngày bắt đầu!" });

                if (promo.PromoId == 0)
                    _service.AddPromotion(promo);
                else
                    _service.UpdatePromotion(promo);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                _service.DeletePromotion(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
