using Microsoft.AspNetCore.Mvc;
using StoreManagementMVC.Services;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class PaymentController : AdminBaseController
    {
        private readonly PaymentService _service;

        public PaymentController(PaymentService service) => _service = service;

        public IActionResult Index(DateTime? from, DateTime? to, int p = 1)
        {
            var result = _service.GetPaymentsPaging(p, 10, from, to);

            ViewBag.CurrentPage = p;
            ViewBag.TotalPages = (int)Math.Ceiling((double)result.totalCount / 10);
            ViewBag.FromDate = from?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to?.ToString("yyyy-MM-dd");
            ViewBag.TotalRevenue = _service.GetTotalRevenue(); // Hiển thị tổng tiền

            return View(result.payments);
        }
    }
}
