using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store.Shared;
using Store.Shared.DTOs;
using Store.Shared.Entities;
using StoreManagementMVC.Data;


namespace StoreManagementMVC.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderApiController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/OrderApi
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDTO orderDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // --- TẠO KHÁCH HÀNG ---
                // Ánh xạ từ DTO 
                var customer = new Customer
                {
                    Name = orderDto.CustomerName,   
                    Phone = orderDto.PhoneNumber,  
                    Address = orderDto.Address,     
                    Email = null,                   // Form hiện tại chưa có nhập Email nên để null
                    CreatedAt = DateTime.Now
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync(); // Lưu ngay để SQL sinh ra CustomerId

                // --- TẠO ĐƠN HÀNG ---
                var order = new Order
                {
                    CustomerId = customer.CustomerId, // Lấy ID của khách vừa tạo
                    OrderDate = DateTime.Now,
                    Status = "pending", // Trạng thái mặc định: Chờ xử lý
                    UserId = null,      // Khách vãng lai

                    DiscountAmount = 0,
                    PromoId = null,
                    TotalAmount = 0 // Tính tổng tiền tạm tính
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Lưu ngay để lấy OrderId

                // --- XỬ LÝ CHI TIẾT & TRỪ KHO ---
                decimal calculatedTotal = 0; // Biến dùng để cộng dồn tổng tiền hàng

                foreach (var item in orderDto.CartItems)
                {
                    // Tính toán giá trị
                    decimal lineTotal = item.Price * item.Quantity;
                    calculatedTotal += lineTotal;

                    // Tạo OrderItem
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId, // Link về Order ở trên
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Subtotal = lineTotal // Lưu thành tiền của dòng này
                    };
                    _context.OrderItems.Add(orderItem);

                    // Trừ kho
                    var inventory = _context.Inventories.FirstOrDefault(i => i.ProductId == item.ProductId);
                    if (inventory == null)
                    {
                        throw new Exception($"Sản phẩm ID {item.ProductId} không tồn tại trong kho!");
                    }
                    if (inventory.Quantity < item.Quantity)
                    {
                        throw new Exception($"Sản phẩm {item.ProductName} không đủ hàng (Còn: {inventory.Quantity})!");
                    }

                    inventory.Quantity -= item.Quantity; // Trừ số lượng thực tế
                    // Tùy chọn: update cả inventory.UpdatedAt = DateTime.Now 
                }
                // --- XỬ LÝ MÃ GIẢM GIÁ ---
                if (orderDto.PromoId.HasValue)
                {
                    var promo = await _context.Promotions.FindAsync(orderDto.PromoId.Value);
                    if (promo != null
                && promo.Status == "active"
                && DateTime.Now >= promo.StartDate
                && DateTime.Now <= promo.EndDate
                && calculatedTotal >= promo.MinOrderAmount)
                    {
                        // Check lượt dùng 
                        if (promo.UsageLimit > 0 && promo.UsedCount >= promo.UsageLimit)
                        {
                            throw new Exception("Mã giảm giá đã hết lượt sử dụng!");
                        }

                        // Tính toán số tiền được giảm
                        decimal discountAmt = 0;
                        if (promo.DiscountType == "percent")
                        {
                            discountAmt = calculatedTotal * (promo.DiscountValue / 100);
                        }
                        else // "fixed"
                        {
                            discountAmt = promo.DiscountValue;
                        }

                        // Không giảm quá tổng tiền đơn hàng
                        if (discountAmt > calculatedTotal) discountAmt = calculatedTotal;

                        // Cập nhật vào Order
                        order.PromoId = promo.PromoId;
                        order.DiscountAmount = discountAmt;

                        // Tăng số lượt đã dùng của mã này lên 1
                        promo.UsedCount += 1;
                    }
                }

                // --- CHỐT TỔNG TIỀN VÀ LƯU ---
                // Tổng tiền cuối = Tổng tiền hàng - Giảm giá
                order.TotalAmount = calculatedTotal - order.DiscountAmount;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Chốt giao dịch: Lưu tất cả xuống DB

                return Ok(new { OrderId = order.OrderId, Message = "Đặt hàng thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Có lỗi thì hoàn tác, không lưu rác
                return BadRequest(new { Message = ex.Message });
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
