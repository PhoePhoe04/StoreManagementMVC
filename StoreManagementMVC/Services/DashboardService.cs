using StoreManagement.Data;

namespace StoreManagement.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _context;
        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public int GetTotalProducts() => _context.Products.Count();
        public int GetTotalOrders() => _context.Orders.Count();
        public int GetTotalCustomers() => _context.Customers.Count();

        public decimal GetTotalRevenue()
        {
            return _context.Orders
                .Where(o => o.Status == "paid")
                .Sum(o => o.TotalAmount);   
        }
    }
}
