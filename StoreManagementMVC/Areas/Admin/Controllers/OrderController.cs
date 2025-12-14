using Microsoft.AspNetCore.Mvc;
using StoreManagementMVC.Services;
using StoreManagementMVC.Models;
using Store.Shared;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class OrderController : Controller
    {
        private readonly OrderService _service;
        private readonly CustomerService _customerService;
        private readonly ProductService _productService;
        private readonly PromotionService _promoService;

        public OrderController(OrderService service, 
                               CustomerService customerService, 
                               ProductService productService,
                               PromotionService promotionService)
        {
            _service = service;
            _customerService = customerService;
            _productService = productService;
            _promoService = promotionService;
        }

        // GET: /Order
        public IActionResult Index(int p = 1, string search = "")
        {
            int pageSize = 10;

            var result = _service.GetOrdersPaging(p, pageSize, search);

            ViewBag.CurrentPage = p;
            ViewBag.TotalPages = (int)Math.Ceiling((double)result.totalCount / 10);
            ViewBag.Search = search;

            return View(result.orders);
        }

        // GET: /Order/Details/5
        public IActionResult Details(int id)
        {
            var order = _service.GetOrderById(id);
            if (order == null) return NotFound();

            return View(order);
        }

        // POST: /Order/UpdateStatus (AJAX đổi trạng thái)
        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            try
            {
                _service.UpdateOrderStatus(id, status);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult Create()
        {
            // Lấy danh sách khách hàng
            var customersData = _customerService.GetCustomersPaging(1, 100).customers;

            // Kiểm tra null an toàn hơn
            ViewBag.Customers = customersData ?? new List<Customer>();

            return View();
        }

        // POST: /Order/Create
        [HttpPost]
        public IActionResult Create([FromBody] OrderCreationVM model)
        {
            try
            {
                if (model == null || model.Items == null || model.Items.Count == 0)
                    return Json(new { success = false, message = "Chưa chọn sản phẩm nào!" });

                int userId = 1; // Admin mặc định

                int newOrderId = _service.CreateOrder(model, userId);
                return Json(new { success = true, orderId = newOrderId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API tìm sản phẩm cho việc bán hàng (Auto-complete)
        [HttpGet]
        public IActionResult SearchProduct(string term)
        {
            var products = _productService.SearchProducts(term);

            var result = products.Select(p => new
            {
                id = p.ProductId,
                label = $"{p.ProductName} - {p.Price.ToString("N0")}đ",
                value = p.ProductName,
                price = p.Price,
                name = p.ProductName
            });

            return Json(result);
        }

        [HttpGet]
        public IActionResult CheckPromotion(string code, decimal total)
        {
            try
            {
                var promo = _promoService.CheckValidPromotion(code, total);

                // Tính số tiền được giảm
                decimal discountAmount = 0;
                if (promo.DiscountType == "percent")
                {
                    discountAmount = total * (promo.DiscountValue / 100);
                }
                else
                {
                    discountAmount = promo.DiscountValue;
                }

                // Không được giảm quá tổng tiền (tránh âm tiền)
                if (discountAmount > total) discountAmount = total;

                return Json(new { success = true, discount = discountAmount, promoId = promo.PromoId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
