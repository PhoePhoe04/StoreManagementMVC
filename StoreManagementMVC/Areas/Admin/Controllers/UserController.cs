using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Entities;
using StoreManagementMVC.Data;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class UserController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách nhân viên
        public IActionResult Index()
        {
            var users = _context.Users.OrderByDescending(u => u.CreatedAt).ToList();
            return View(users);
        }

        // Hiện Modal Thêm/Sửa
        public IActionResult Upsert(int? id)
        {
            // Nếu không có ID -> Thêm mới -> Trả về User rỗng
            if (id == null || id == 0)
            {
                return PartialView("_Upsert", new User());
            }

            // Nếu có ID -> Sửa -> Lấy từ DB
            var user = _context.Users.Find(id.Value);
            if (user == null) return NotFound();

            // Khi sửa, ta xóa password ở model đi để ô input trống
            user.Password = "";

            return PartialView("_Upsert", user);
        }

        // Xử lý Lưu (Create/Update)
        [HttpPost]
        public IActionResult Upsert(User model)
        {

            if (model.UserId > 0)
            {
                // Nếu đang Sửa mà ô Password bỏ trống
                if (string.IsNullOrEmpty(model.Password))
                {
                    // Lấy password cũ từ DB đắp vào
                    var oldUser = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserId == model.UserId);
                    if (oldUser != null)
                    {
                        model.Password = oldUser.Password;
                        // Xóa lỗi validation vì ta đã điền pass cũ vào rồi
                        ModelState.Remove("Password");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                if (model.UserId == 0)
                {
                    // Thêm mới
                    // Nên mã hóa mật khẩu (MD5/BCrypt) trước khi lưu, ở đây không mã hóa mà lưu thẳng vào DB
                    model.CreatedAt = DateTime.Now;
                    _context.Users.Add(model);
                }
                else
                {
                    // Cập nhật
                    // Giữ nguyên ngày tạo cũ
                    var oldUser = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserId == model.UserId);
                    if (oldUser != null) model.CreatedAt = oldUser.CreatedAt;

                    _context.Users.Update(model);
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại!" });
        }

        // Xóa nhân viên
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy nhân viên!" });

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}
