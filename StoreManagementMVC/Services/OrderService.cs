using Microsoft.EntityFrameworkCore;
using StoreManagementMVC.Data;
using StoreManagementMVC.Models;
using Store.Shared;

namespace StoreManagementMVC.Services
{
    public class OrderService
    {
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context) => _context = context;

        // Lấy danh sách đơn hàng
        public (List<Order> orders, int totalCount) GetOrdersPaging(int pageIndex, int pageSize, string keyword = "")
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                if (int.TryParse(keyword, out int orderId))
                {
                    query = query.Where(o => o.OrderId == orderId);
                }
                else
                {
                    query = query.Where(o => o.Customer.Name.Contains(keyword));
                }
            }

            int totalCount = query.Count();

            var list = query.OrderByDescending(o => o.OrderDate)
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            return (list, totalCount);
        }

        // Lấy chi tiết đơn hàng
        public Order? GetOrderById(int id)
        {
            return _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.OrderId == id);
        }

        // Cập nhật trạng thái
        public void UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = _context.Orders.Find(orderId);
            if (order != null)
            {
                order.Status = newStatus;
                _context.SaveChanges();
            }
        }

        // Xóa đơn hàng
        public void DeleteOrder(int id)
        {
            var order = _context.Orders.Find(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                _context.SaveChanges();
            }
        }

        // Tạo đơn hàng mới (Transaction)
        public int CreateOrder(OrderCreationVM model, int userId)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var order = new Order
                {
                    CustomerId = model.CustomerId,
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    Status = "pending",
                    OrderItems = new List<OrderItem>()
                };

                decimal totalAmount = 0;

                foreach (var item in model.Items)
                {
                    decimal subtotal = item.Quantity * item.Price;
                    totalAmount += subtotal;

                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Subtotal = subtotal
                    };
                    order.OrderItems.Add(orderItem);

                    // --- Kiểm tra tồn kho ---
                    var inventory = _context.Inventories
                        .FirstOrDefault(i => i.ProductId == item.ProductId);

                    // Trường hợp 1: Sản phẩm chưa từng được nhập kho (chưa có row trong bảng Inventory)
                    if (inventory == null)
                    {
                        // Bạn có thể chọn: Báo lỗi HOẶC Tự tạo kho mới với số lượng âm (cho phép bán trước)
                        // Ở đây tôi giữ nguyên logic báo lỗi của bạn cho chặt chẽ
                        throw new Exception($"Sản phẩm (ID: {item.ProductId}) chưa có dữ liệu tồn kho!");
                    }

                    // Trường hợp 2: Có kho nhưng không đủ số lượng
                    if (inventory.Quantity < item.Quantity)
                    {
                        throw new Exception($"Sản phẩm (ID: {item.ProductId}) không đủ hàng! Tồn: {inventory.Quantity}, Cần: {item.Quantity}");
                    }

                    // Trừ kho
                    inventory.Quantity -= item.Quantity;
                    _context.Inventories.Update(inventory);
                }

                decimal discountAmount = 0;
                if (!string.IsNullOrEmpty(model.PromoCode))
                {
                    // GỌI LẠI HÀM CHECK (Server phải tự check lại, ko tin client)
                    var promoService = new PromotionService(_context); // Hoặc inject vào
                    var promo = promoService.CheckValidPromotion(model.PromoCode, totalAmount);

                    if (promo != null)
                    {
                        if (promo.DiscountType == "percent")
                            discountAmount = totalAmount * (promo.DiscountValue / 100);
                        else
                            discountAmount = promo.DiscountValue;

                        // Cập nhật số lần dùng
                        promo.UsedCount++;
                        _context.Promotions.Update(promo);

                        order.PromoId = promo.PromoId; // Lưu ID khuyến mãi vào đơn
                    }
                }

                order.DiscountAmount = discountAmount;
                order.TotalAmount = totalAmount - discountAmount;

                _context.Orders.Add(order);
                _context.SaveChanges();

                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    Amount = order.TotalAmount, // Giả sử khách trả đủ 100%
                    PaymentMethod = model.PaymentMethod,
                    PaymentDate = DateTime.Now
                };
                _context.Payments.Add(payment);

                // Cập nhật trạng thái đơn thành 'paid' luôn vì đã trả tiền
                order.Status = "paid";

                _context.SaveChanges(); // Lưu lần 2 (Payment + Update Order Status)

                transaction.Commit();
                return order.OrderId;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }


    }
}
