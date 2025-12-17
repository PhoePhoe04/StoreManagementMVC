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
            // Tìm user theo Username trước
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            // Kiểm tra nếu user không tồn tại
            if (user == null)
            {
                ViewBag.Error = "Tài khoản không tồn tại!";
                return View();
            }

            // Kiểm tra Mật khẩu 
           
            if (user.Password != password)
            {
                ViewBag.Error = "Sai mật khẩu!";
                return View();
            }

            // KIỂM TRA QUYỀN 
            // Chuyển về chữ thường để so sánh cho chính xác (tránh Admin vs admin)
            string role = user.Role?.Trim().ToLower() ?? "";

            if (role != "admin" && role != "staff")
            {
                ViewBag.Error = "Tài khoản của bạn không có quyền truy cập trang quản trị!";
                return View();
            }

            // Tạo Claims để lưu phiên đăng nhập
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName ?? ""),
                new Claim(ClaimTypes.Role, role), // Lưu role đã chuẩn hóa
                new Claim("UserId", user.UserId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Ghi Cookie (Đăng nhập thành công)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            // Chuyển hướng về trang cũ hoặc trang chủ Admin
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
