using Microsoft.AspNetCore.Mvc;
using Store.Shared.Entities;
using StoreManagementMVC.Data;

namespace StoreManagementMVC.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerApiController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/CustomerApi/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        // PUT: api/CustomerApi/1
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] Customer customer)
        {
            if (id != customer.CustomerId) return BadRequest();

            var existing = await _context.Customers.FindAsync(id);
            if (existing == null) return NotFound();

            // Cập nhật thông tin
            existing.Name = customer.Name;
            existing.Phone = customer.Phone;
            existing.Address = customer.Address;
            existing.Email = customer.Email;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Cập nhật thành công!" });
        }
    }
}
