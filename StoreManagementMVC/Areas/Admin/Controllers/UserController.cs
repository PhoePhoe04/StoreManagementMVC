using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Entities;
using StoreManagementMVC.Services;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class UserController : AdminBaseController
    {
        private readonly UserService _service;

        public UserController(UserService service)
        {
            _service = service;
        }

        // Danh sách (Có hỗ trợ type: employee/customer)
        public IActionResult Index(int p = 1, string search = "", string type = "employee")
        {
            try
            {
                var result = _service.GetUsersPaging(p, 10, search, type);

                ViewBag.CurrentPage = p;
                ViewBag.TotalPages = (int)Math.Ceiling((double)result.totalCount / 10);
                ViewBag.Search = search;

                // Truyền Type sang View để giữ trạng thái khi chuyển trang
                ViewBag.Type = type;
                ViewBag.Title = type == "customer" ? "Danh sách Khách hàng (User)" : "Danh sách Nhân viên";

                return View(result.users);
            }
            catch (Exception)
            {
                return View(new List<User>());
            }
        }

        // Hiện Modal Thêm/Sửa
        public IActionResult Upsert(int? id, string type = "employee")
        {
            // Truyền Type sang Modal để hiển thị Dropdown Role cho đúng
            ViewBag.Type = type;

            if (id == null || id == 0)
            {
                // Tạo mới: Mặc định Role dựa theo Type
                var newUser = new User
                {
                    Role = (type == "customer") ? "customer" : "staff"
                };
                return PartialView("_Upsert", newUser);
            }

            var user = _service.GetUserById(id.Value);
            if (user == null) return NotFound();

            // Xóa password để ô input trống (bảo mật)
            user.Password = "";
            return PartialView("_Upsert", user);
        }

        // Xử lý Lưu
        [HttpPost]
        public IActionResult Upsert(User model)
        {
            // Nếu password trống khi Edit, ta xóa lỗi ModelState để nó Valid (vì Service sẽ tự lấy pass cũ)
            if (model.UserId > 0 && string.IsNullOrEmpty(model.Password))
            {
                ModelState.Remove("Password");
            }

            // Email có thể null, nên nếu null thì ModelState vẫn Valid (do ta khai báo string? Email)

            if (ModelState.IsValid)
            {
                try
                {
                    _service.SaveUser(model);
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            // Lấy lỗi chi tiết từ ModelState để báo về Client
            var errors = string.Join("<br/>", ModelState.Values
                                    .SelectMany(v => v.Errors)
                                    .Select(e => e.ErrorMessage));

            return Json(new { success = false, message = "Dữ liệu không hợp lệ:<br/>" + errors });
        }

        // Xóa
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                _service.DeleteUser(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
