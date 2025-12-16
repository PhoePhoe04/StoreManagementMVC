using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Entities;
using StoreManagementMVC.Data;

namespace StoreManagementMVC.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/CategoryApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            // Chỉ lấy các danh mục nào có sản phẩm để hiển thị cho gọn
            return await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }
    }
}
