using Microsoft.AspNetCore.Mvc;
using StoreManagement.Data;
using StoreManagement.Models;

namespace StoreManagementMVC.Controllers
{
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
        public IActionResult Upsert(Category category)
        {
            if(category.CategoryId == 0)
                _context.Categories.Add(category);
            else
                _context.Categories.Remove(category);

            _context.SaveChanges();
            return Json(new { success = true });
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
