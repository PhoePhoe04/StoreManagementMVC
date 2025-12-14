using Store.Shared;

namespace StoreManagementMVC.Models
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }
}
