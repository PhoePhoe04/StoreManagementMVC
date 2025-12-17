using Microsoft.AspNetCore.Mvc;
using StoreManagementMVC.Data;
using Store.Shared.Entities;
using StoreManagementMVC.Services;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class CategoryController : AdminBaseController
    {
        private readonly CategoryService _service;

        // Inject Service
        public CategoryController(CategoryService service)
        {
            _service = service;
        }

        // GET: /Category/Index
        public IActionResult Index(int p = 1, string search = "")
        {
            // Gọi Service lấy dữ liệu phân trang + tìm kiếm
            var result = _service.GetCategoriesPaging(p, 10, search);

            ViewBag.CurrentPage = p;
            ViewBag.TotalPages = (int)Math.Ceiling((double)result.totalCount / 10);
            ViewBag.Search = search;

            return View(result.categories);
        }

        // Hiện Modal Upsert
        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                return PartialView("_Upsert", new Category());
            }

            var category = _service.GetCategoryById(id.Value);
            if (category == null) return NotFound();

            return PartialView("_Upsert", category);
        }

        // POST: Upsert (Lưu)
        [HttpPost]
        public IActionResult Upsert(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _service.SaveCategory(category);
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            return Json(new { success = false, message = "Dữ liệu nhập vào không hợp lệ!" });
        }

        // POST: Delete
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                _service.DeleteCategory(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
