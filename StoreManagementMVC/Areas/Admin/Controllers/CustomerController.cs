using Microsoft.AspNetCore.Mvc;
using Store.Shared.Entities;
using StoreManagementMVC.Services;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class CustomerController : Controller
    {
        private readonly CustomerService _service;

        public CustomerController(CustomerService service) => _service = service;

        // GET: /Service/Index
        public IActionResult Index(int p = 1, string search = "")
        {
            var result = _service.GetCustomersPaging(p, 7, search);

            ViewBag.CurrentPage = p;
            ViewBag.TotalPages = (int)Math.Ceiling((double)result.totalCount / 10);
            ViewBag.Search = search;

            return View(result.customers);
        }

        // GET: /Service/Upsert (Trả về Partial View cho Modal)
        public IActionResult Upsert(int? id)
        {
            if (id == null) return PartialView("_Upsert", new Customer());

            var customer = _service.GetCustomerById(id.Value);
            if (customer == null) return NotFound();
            
            return PartialView("_Upsert", customer);
        }

        [HttpPost]
        public IActionResult Upsert(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                if (customer.CustomerId == 0)
                    _service.AddCustomer(customer);
                else
                    _service.UpdateCustomer(customer);

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
                _service.DeleteCustomer(id);
                return Json(new { success = true , message = "Xóa khách hàng thành công"});
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể xóa khách hàng này (có thể do đang có đơn hàng)." });
            }
        }
    }
}
