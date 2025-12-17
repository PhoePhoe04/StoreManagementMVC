using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Shared.Entities;
using StoreManagementMVC.Data;

namespace StoreManagementMVC.Controllers.Api
{
    //[Route("api/[controller]")]
    [Route("api/customer")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CustomerApiController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/customer/profile
        // Lấy thông tin khách hàng dựa trên Token đăng nhập
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // 1. Lấy UserId từ Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            // 2. Tìm Customer có UserId tương ứng
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null) return NotFound("Chưa tìm thấy thông tin khách hàng.");

            return Ok(customer);
        }

        // PUT: api/customer/profile
        // Cập nhật thông tin
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(Customer request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return NotFound();

            // Cập nhật các trường cho phép
            customer.Name = request.Name;
            customer.Phone = request.Phone;
            customer.Address = request.Address;
            // Không cho sửa Email nếu Email dùng để login/recovery

            await _context.SaveChangesAsync();
            return Ok(customer);
        }
    }
}
