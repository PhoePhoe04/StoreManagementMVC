using Microsoft.EntityFrameworkCore;
using StoreManagementMVC.Data;
using Store.Shared;

namespace StoreManagementMVC.Services
{
    public class PaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context) => _context = context;

        // 1. Lấy danh sách thanh toán (Phân trang + Lọc theo ngày)
        public (List<Payment> payments, int totalCount) GetPaymentsPaging(int pageIndex, int pageSize, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Customer) // Lấy tên khách để hiển thị
                .AsQueryable();

            // Lọc theo ngày
            if (fromDate.HasValue) query = query.Where(p => p.PaymentDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(p => p.PaymentDate <= toDate.Value.AddDays(1).AddTicks(-1)); // Hết ngày

            int totalCount = query.Count();

            var list = query.OrderByDescending(p => p.PaymentDate)
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            return (list, totalCount);
        }

        // 2. Thêm thanh toán (Dùng khi thanh toán công nợ hoặc tạo đơn)
        public void AddPayment(int orderId, decimal amount, string method)
        {
            var order = _context.Orders.Find(orderId);
            if (order == null) throw new Exception("Đơn hàng không tồn tại");

            var payment = new Payment
            {
                OrderId = orderId,
                Amount = amount,
                PaymentMethod = method,
                PaymentDate = DateTime.Now
            };

            _context.Payments.Add(payment);

            // Kiểm tra nếu thanh toán đủ tiền thì đổi trạng thái đơn thành 'paid'
            var totalPaid = _context.Payments.Where(p => p.OrderId == orderId).Sum(p => p.Amount) + amount;
            if (totalPaid >= order.TotalAmount)
            {
                order.Status = "paid";
                _context.Orders.Update(order);
            }

            _context.SaveChanges();
        }

        // 3. Thống kê tổng doanh thu
        public decimal GetTotalRevenue() => _context.Payments.Sum(p => p.Amount);
    }
}
