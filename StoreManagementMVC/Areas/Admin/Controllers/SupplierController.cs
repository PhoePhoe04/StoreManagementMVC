using Microsoft.AspNetCore.Mvc;
using Store.Shared.Entities;
using StoreManagementMVC.Services;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class SupplierController : AdminBaseController
    {
        private readonly SupplierService _service;

        public SupplierController(SupplierService service)
        {
            _service = service;
        }

        // GET: /Supplier/Index
        public IActionResult Index(int p = 1, string search = "")
        {
            var result = _service.GetSuppliersPaging(p, 10, search);

            ViewBag.CurrentPage = p;
            ViewBag.TotalPages = (int)Math.Ceiling((double)result.totalCount / 10);
            ViewBag.Search = search;

            return View(result.suppliers);
        }

        // GET: /Supplier/Upsert (Mở Modal)
        public IActionResult Upsert(int? id)
        {
            if (id == null) return PartialView("_Upsert", new Supplier());

            var supplier = _service.GetSupplierById(id.Value);
            if (supplier == null) return NotFound();

            return PartialView("_Upsert", supplier);
        }

        // POST: /Supplier/Upsert 
        [HttpPost]
        public IActionResult Upsert(Supplier supplier)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                if (supplier.SupplierId == 0)
                    _service.AddSupplier(supplier);
                else
                    _service.UpdateSupplier(supplier);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Supplier/Delete
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                _service.DeleteSupplier(id);
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Không thể xóa NCC này (đang có sản phẩm liên kết)." });
            }
        }
    }
}
