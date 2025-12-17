using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Shared;
using Store.Shared.DTOs;
using Store.Shared.Entities;
using StoreManagementMVC.Data;


namespace StoreManagementMVC.Controllers.Api
{
    //[Route("api/[controller]")]
    [Route("api/order")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class OrderApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/order/my-orders
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            // Lấy UserId
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            // Tìm CustomerId trước
            var customer = await _context.Customers
                                .Select(c => new { c.CustomerId, c.UserId })
                                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null) return Ok(new List<object>()); // Chưa mua hàng bao giờ

            // Lấy danh sách đơn hàng của Customer đó
            // (Giả sử bạn có bảng Orders liên kết với CustomerId)
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customer.CustomerId)
                .OrderByDescending(o => o.OrderDate) // Đơn mới nhất lên đầu
                .ToListAsync();

            return Ok(orders);
        }


        // POST: api/order/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            // 1. Lấy UserId từ Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            // 2. Tìm Customer
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
                return BadRequest("Bạn cần cập nhật thông tin cá nhân (Profile) trước khi đặt hàng.");

            if (request.CartItems == null || request.CartItems.Count == 0)
                return BadRequest("Giỏ hàng trống.");

            // 3. TRANSACTION (Đảm bảo toàn vẹn dữ liệu)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3.1. Tính tổng tiền hàng (Subtotal)
                decimal subtotal = request.CartItems.Sum(x => x.Price * x.Quantity);
                decimal discountAmount = 0;

                // 3.2. Xử lý Mã giảm giá
                int? appliedPromoId = null;
                if (!string.IsNullOrEmpty(request.PromotionCode))
                {
                    var today = DateTime.Now;
                    var promo = await _context.Promotions
                        .FirstOrDefaultAsync(p => p.PromoCode == request.PromotionCode && p.Status == "active");
                    
                    // Validate kỹ lại lần nữa ở Server (Trust no one)
                    if (promo != null)
                    {
                        // Check hạn dùng
                        if (today < promo.StartDate || today > promo.EndDate)
                            throw new Exception("Mã giảm giá đã hết hạn.");

                        // Check số lượng
                        if (promo.UsageLimit > 0 && promo.UsedCount >= promo.UsageLimit)
                            throw new Exception("Mã giảm giá đã hết lượt dùng.");

                        // Check đơn tối thiểu
                        if (subtotal < promo.MinOrderAmount)
                            throw new Exception($"Đơn hàng chưa đạt tối thiểu {promo.MinOrderAmount:N0}đ để dùng mã này.");

                        // TÍNH TOÁN SỐ TIỀN GIẢM
                        if (promo.DiscountType == "percent")
                        {
                            discountAmount = subtotal * (promo.DiscountValue / 100);
                        }
                        else // "fixed"
                        {
                            discountAmount = promo.DiscountValue;
                        }

                        // Đảm bảo không giảm quá số tiền đơn hàng (tránh bị âm tiền)
                        if (discountAmount > subtotal) discountAmount = subtotal;

                        // Tăng số lượt đã dùng lên 1
                        promo.UsedCount++;
                        appliedPromoId = promo.PromoId;
                    }
                }

                /// 3.3. Tạo Order
                var order = new Order
                {
                    CustomerId = customer.CustomerId,
                    OrderDate = DateTime.Now,
                    Status = "Pending",
                    // Lưu tổng tiền sau khi trừ
                    TotalAmount = subtotal - discountAmount,
                    DiscountAmount = discountAmount, // Lưu số tiền được giảm
                    PromoId = appliedPromoId         // Lưu ID mã giảm giá (có thể null)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Lưu ngay để lấy OrderId (ví dụ: 105)

                // B. TẠO ORDER ITEMS (Bảng chi tiết con)
                foreach (var item in request.CartItems)
                {
                    // Sử dụng Entity OrderItem của bạn
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,      // FK trỏ về đơn hàng vừa tạo
                        ProductId = item.ProductId,   // FK trỏ về sản phẩm
                        Quantity = item.Quantity,
                        Price = item.Price,           // Giá tại thời điểm mua

                        // Tính Subtotal (Thành tiền = Giá x Số lượng)
                        Subtotal = item.Price * item.Quantity
                    };

                    // Lưu ý: Cần đảm bảo trong AppDbContext bạn đã khai báo DbSet<OrderItem>
                    _context.OrderItems.Add(orderItem);

                    // C. TRỪ TỒN KHO (Logic kiểm tra hàng tồn)
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        // Kiểm tra null cho Inventory để tránh lỗi
                        if (product.Inventory != null)
                        {
                            if (product.Inventory.Quantity >= item.Quantity)
                            {
                                product.Inventory.Quantity -= item.Quantity;
                            }
                            else
                            {
                                throw new Exception($"Sản phẩm {product.ProductName} không đủ số lượng tồn kho (Còn: {product.Inventory.Quantity}).");
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync(); // Lưu tất cả OrderItem và cập nhật kho
                await transaction.CommitAsync();   // Chốt giao dịch thành công

                return Ok(new { orderId = order.OrderId, message = "Đặt hàng thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Có lỗi thì hoàn tác hết
                return StatusCode(500, "Lỗi đặt hàng: " + ex.Message);
            }
        }

        // GET: api/OrderApi/history/1
        [HttpGet("history/{customerId}")]
        public async Task<IActionResult> GetOrderHistory(int customerId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems) // Kèm chi tiết sản phẩm
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate) // Đơn mới nhất lên đầu
                .ToListAsync();

            return Ok(orders);
        }
    }
}
