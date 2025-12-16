using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagementMVC.Data;

namespace StoreManagementMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Account/Login
        [AllowAnonymous] // Cho phép chưa đăng nhập cũng vào được để nhập pass
        [HttpGet]
        public IActionResult Login(string? ReturnUrl = null)
        {
            ViewData["ReturnUrl"] = ReturnUrl;
            return View();
        }

        // POST: /Admin/Account/Login
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? ReturnUrl)
        {
            // Kiểm tra username/password trong DB
            // Lưu ý: Ở đây đang so sánh chuỗi thô.
            // Thực tế nên mã hóa password (MD5/BCrypt) để bảo mật.
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
                return View();
            }

            // Tạo danh sách các thông tin (Claims) để lưu vào Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName ?? ""),
                new Claim(ClaimTypes.Role, user.Role), // Lưu Role (admin/staff)
                new Claim("UserId", user.UserId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Ghi Cookie xuống trình duyệt (Đăng nhập thành công)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            // Chuyển hướng
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl); // Quay lại trang định vào trước đó
            }

            return RedirectToAction("Index", "Home", new { area = "Admin" }); // Mặc định về trang chủ Admin
        }

        // Đăng xuất
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }

        // Trang báo lỗi khi không có quyền
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
