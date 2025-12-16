using Microsoft.AspNetCore.Mvc;
using StoreManagementMVC.Data;
using Store.Shared.Entities;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Category
        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        // Upsert (Thêm/Sửa) - Gọi Modal
        public IActionResult Upsert(int? id)
        {
            if(id == null) return PartialView("_Upsert", new Category());

            var category = _context.Categories.Find(id.Value);
            if (category == null) return NotFound();

            return PartialView("_Upsert", category);
        }

        // POST: Upsert
        [HttpPost]
        [HttpPost]
        public IActionResult Upsert(Category category)
        {
            // Kiểm tra dữ liệu hợp lệ 
            if (ModelState.IsValid)
            {
                if (category.CategoryId == 0)
                {
                    // Thêm mới
                    _context.Categories.Add(category);
                }
                else
                {
                    // Cập nhật
                    _context.Categories.Update(category);
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }

            // Trả về lỗi nếu dữ liệu không hợp lệ
            return Json(new { success = false, message = "Dữ liệu nhập vào không hợp lệ!" });
        }

        // POST: Delete
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if(category == null) return Json(new { success = false, message = "Không tìm thấy!" });

            _context.Categories.Remove(category);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}
