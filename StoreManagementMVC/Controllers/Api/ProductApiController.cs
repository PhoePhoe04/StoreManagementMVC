using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementMVC.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Store.Shared.Entities;
using Store.Shared.DTOs;

namespace StoreManagementMVC.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/ProductApi
        // Lấy danh sách (Có hỗ trợ tìm kiếm và lọc theo danh mục)
        [HttpGet]
        public async Task<ActionResult<ProductResult>> GetProducts(
            string? search, 
            int? categoryId,
            decimal? minPrice, // 1. Giá thấp nhất
            decimal? maxPrice, // 2. Giá cao nhất
            string? sort,      // 3. Kiểu sắp xếp (asc/desc)
            int page = 1)
        {
            // Cố định sản phẩm mỗi trang
            int pageSize = 9;

            // QUERY GỐC: Kết nối Product + Inventory + Category
            var query = from p in _context.Products
                        // Left Join với Inventory để sản phẩm chưa nhập kho vẫn hiện ra
                        join i in _context.Inventories on p.ProductId equals i.ProductId into invGroup
                        from inv in invGroup.DefaultIfEmpty()
                        // Join với Category để lấy tên danh mục
                        join c in _context.Categories on p.CategoryId equals c.CategoryId
                        select new
                        {
                            p,   // Giữ lại Product để lọc
                            inv, // Giữ lại Inventory để lấy số lượng
                            c    // Giữ lại Category
                        };

            // --- TÌM KIẾM - LỌC THEO TÊN ---
            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.p.ProductName.Contains(search));

            // --- LỌC DANH MỤC ---
            if (categoryId.HasValue)
                query = query.Where(x => x.p.CategoryId == categoryId);

            // --- LỌC THEO GIÁ  ---
            if (minPrice.HasValue)
                query = query.Where(x => x.p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(x => x.p.Price <= maxPrice.Value);

            // --- SẮP XẾP ---
            switch (sort)
            {
                case "price_asc": query = query.OrderBy(x => x.p.Price); break;
                case "price_desc": query = query.OrderByDescending(x => x.p.Price); break;
                default: query = query.OrderByDescending(x => x.p.ProductId); break; // Mặc định sắp xếp theo mới nhất
            }

            // --- PHÂN TRANG ---
            int totalCount = await query.CountAsync();

            var dataList = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProductDTO // <--- CHUYỂN ĐỔI SANG DTO TẠI ĐÂY
                {
                    ProductId = x.p.ProductId,
                    ProductName = x.p.ProductName,
                    Price = x.p.Price,
                    CategoryId = x.p.CategoryId ?? 0,
                    CategoryName = x.c != null ? x.c.CategoryName : "Chưa phân loại",

                    // Logic lấy số lượng: Nếu inv có dữ liệu thì lấy Quantity, không thì bằng 0
                    Quantity = x.inv != null ? x.inv.Quantity : 0
                })
                .ToListAsync();


            // Trả về kết quả
            return new ProductResult { Products = dataList, TotalCount = totalCount };
        }


        [HttpGet("featured")]
        public async Task<ActionResult<List<ProductDTO>>> GetFeaturedProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                // Join Inventory để lấy số lượng
                .GroupJoin(_context.Inventories,
                    p => p.ProductId,
                    i => i.ProductId,
                    (p, invGroup) => new { p, inv = invGroup.FirstOrDefault() })
                // Sắp xếp ngẫu nhiên
                .OrderBy(x => Guid.NewGuid())
                .Take(4) // Lấy 4 cái
                .Select(x => new ProductDTO
                {
                    ProductId = x.p.ProductId,
                    ProductName = x.p.ProductName,
                    Price = x.p.Price,
                    CategoryId = x.p.CategoryId ?? 0,
                    CategoryName = x.p.Category != null ? x.p.Category.CategoryName : "",
                    Quantity = x.inv != null ? x.inv.Quantity : 0
                })
                .ToListAsync();

            return products;
        }

        [HttpGet("latest")]
        public async Task<ActionResult<List<ProductDTO>>> GetLatestProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                // Join Inventory
                .GroupJoin(_context.Inventories,
                    p => p.ProductId,
                    i => i.ProductId,
                    (p, invGroup) => new { p, inv = invGroup.FirstOrDefault() })
                // Sắp xếp ID giảm dần (Mới nhất)
                .OrderByDescending(x => x.p.ProductId)
                .Take(8) // Lấy 8 cái
                .Select(x => new ProductDTO
                {
                    ProductId = x.p.ProductId,
                    ProductName = x.p.ProductName,
                    Price = x.p.Price,
                    CategoryId = x.p.CategoryId ?? 0,
                    CategoryName = x.p.Category != null ? x.p.Category.CategoryName : "",
                    Quantity = x.inv != null ? x.inv.Quantity : 0
                })
                .ToListAsync();

            return products;
        }


        // GET: api/ProductApi/5
        // Lấy chi tiết 1 sản phẩm
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            return product;
        }
    }
}
