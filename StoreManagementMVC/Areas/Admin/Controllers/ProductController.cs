using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Entities;
using StoreManagementMVC.Services;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class ProductController : AdminBaseController
    {
        private readonly ProductService _service;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ProductService service, IWebHostEnvironment webHostEnvironment)
        {
            _service = service;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Product/Index
        public IActionResult Index(int p = 1)
        {
            var result = _service.GetProductsPaging(p, 10);

            ViewBag.CurrentPage = p;
            ViewBag.TotalPages = (int)Math.Ceiling((double)result.totalCount / 10);

            return View(result.products);
        }

        // GET: /Product/Upsert (Trả về Partial View cho Modal)
        public IActionResult Upsert(int? id)
        {
            // Lấy danh sách danh mục và nhà cung cấp từ Service
            var categories = _service.GetCategories();
            var suppliers = _service.GetSuppliers();

            // Tạo SelectList để truyền sang View (dùng cho thẻ <select>)
            // Tham số: (List nguồn, "Tên cột giá trị lưu xuống DB", "Tên cột hiển thị lên Web")
            ViewBag.CategoryList = new SelectList(categories, "CategoryId", "CategoryName");
            ViewBag.SupplierList = new SelectList(suppliers, "SupplierId", "Name");

            // Trường hợp Thêm mới (id là null)
            if (id == null)
            {
                // Trả về một Product rỗng để người dùng nhập mới
                return PartialView("_Upsert", new Product());
            }

            // Trường hợp Cập nhật (id có giá trị)
            var product = _service.GetProductById(id.Value);

            // Nếu tìm không thấy sản phẩm (ví dụ ai đó xóa rồi) thì báo lỗi 404
            if (product == null)
            {
                return NotFound();
            }

            // Trả về Product cũ để điền sẵn vào các ô input
            return PartialView("_Upsert", product);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            _service.DeleteProduct(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Upsert(Product product, IFormFile? file)
        {
            // --- BƯỚC QUAN TRỌNG: SỬA LỖI TẠI ĐÂY ---
            // Loại bỏ ImageUrl khỏi danh sách kiểm tra lỗi
            // Vì lúc này chưa có ảnh, ta sẽ gán sau.
            ModelState.Remove("ImageUrl");

            // Nếu Inventory là object navigation, đôi khi nó cũng gây lỗi validation
            // Nếu bạn gặp lỗi liên quan đến Inventory, hãy uncomment dòng dưới:
            // ModelState.Remove("Inventory"); 

            // Kiểm tra tính hợp lệ
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                                                .SelectMany(x => x.Errors)
                                                .Select(x => x.ErrorMessage));
                return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + errors });
            }

            try
            {
                // --- BẮT ĐẦU XỬ LÝ ẢNH ---
                if (file != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\products");

                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(productPath))
                    {
                        Directory.CreateDirectory(productPath);
                    }

                    // Xóa ảnh cũ nếu có (trừ ảnh default và khi đang update)
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        // Cần trim dấu \ ở đầu nếu có để tránh lỗi đường dẫn
                        var oldImagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\', '/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Lưu ảnh mới
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    // Cập nhật đường dẫn vào Model
                    // Lưu ý: Dùng dấu gạch chéo ngược \ hoặc gạch chéo / đều được, 
                    // nhưng Web thì nên chuẩn hóa dùng /
                    product.ImageUrl = @"/images/products/" + fileName;
                }
                else
                {
                    // TRƯỜNG HỢP: KHÔNG UPLOAD ẢNH MỚI
                    // Nếu là Thêm mới mà không up ảnh -> Có thể gán ảnh mặc định (nếu muốn)
                    if (product.ProductId == 0 && string.IsNullOrEmpty(product.ImageUrl))
                    {
                        // product.ImageUrl = @"/images/products/default.png"; 
                    }
                }
                // --- KẾT THÚC XỬ LÝ ẢNH ---

                // Gọi Service để lưu dữ liệu
                if (product.ProductId == 0)
                {
                    _service.AddProduct(product);
                }
                else
                {
                    // Đảm bảo Service của bạn cập nhật cả trường ImageUrl
                    
                    _service.UpdateProduct(product);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}
