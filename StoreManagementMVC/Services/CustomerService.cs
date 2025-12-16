using StoreManagementMVC.Data;
using Store.Shared.Entities;

namespace StoreManagementMVC.Services
{
    public class CustomerService
    {
        private readonly AppDbContext _context;
        public CustomerService(AppDbContext context) => _context = context;

        // Lấy danh sách khách hàng với phân trang
        public (List<Customer> customers, int totalCount) GetCustomersPaging(int pageIndex, int pageSize, string keyword = "")
        {
            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(c => c.Name.Contains(keyword) || c.Phone.Contains(keyword));
            }

            int totalCount = query.Count();

            var list = query.OrderByDescending(c => c.CustomerId)
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            return (list, totalCount);
        }

        // Lấy khách hàng theo ID
        public Customer? GetCustomerById(int id)
        {
            return _context.Customers.FirstOrDefault(c => c.CustomerId == id);
        }

        // Thêm khách hàng mới
        public void AddCustomer(Customer customer)
        {
            customer.CreatedAt = DateTime.Now;
            _context.Customers.Add(customer);
            _context.SaveChanges();
        }

        // Cập nhật khách hàng
        public void UpdateCustomer(Customer customer)
        {
            _context.Customers.Update(customer);
            _context.SaveChanges();
        }

        // Xóa khách hàng
        public void DeleteCustomer(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                _context.SaveChanges();
            }
        }
    }
}
