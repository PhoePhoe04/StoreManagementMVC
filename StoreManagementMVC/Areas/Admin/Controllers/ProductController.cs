using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Store.Shared;
using StoreManagementMVC.Services;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class ProductController : Controller
    {
        private readonly ProductService _service;

        public ProductController(ProductService service) => _service = service;

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
        public IActionResult Upsert(Product product)
        {
            // Kiểm tra tính hợp lệ của Model (VD: Tên sản phẩm để trống)
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                                            .SelectMany(x => x.Errors)
                                            .Select(x => x.ErrorMessage));
                return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + errors });
            }

            try
            {
                if (product.ProductId == 0)
                {
                    _service.AddProduct(product);
                }
                else
                {
                    _service.UpdateProduct(product);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Trả về nội dung lỗi cụ thể cho Frontend hiển thị
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}
