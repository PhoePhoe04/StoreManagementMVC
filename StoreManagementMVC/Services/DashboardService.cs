using Microsoft.EntityFrameworkCore;
using StoreManagementMVC.Data;
using StoreManagementMVC.Models;
using Store.Shared;

namespace StoreManagementMVC.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public DashboardViewModel GetDashboardStats()
        {
            var model = new DashboardViewModel
            {
                // 1. Tính tổng doanh thu từ bảng Payments (chính xác nhất)
                TotalRevenue = _context.Payments.Sum(p => p.Amount),

                // 2. Đếm số lượng
                TotalOrders = _context.Orders.Count(),
                TotalProducts = _context.Products.Count(),
                TotalCustomers = _context.Customers.Count(),

                // 3. Lấy 5 đơn hàng mới nhất
                RecentOrders = _context.Orders
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToList()
            };

            return model;
        }
    }
}
