using Microsoft.EntityFrameworkCore;
using Store.Shared.Entities;
using StoreManagementMVC.Data;

namespace StoreManagementMVC.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách (Có phân trang + Lọc theo Type + Tìm kiếm)
        public (List<User> users, int totalCount) GetUsersPaging(int pageIndex, int pageSize, string keyword = "", string type = "employee")
        {
            var query = _context.Users.AsQueryable();

            // Lọc theo từ khóa (Username, Tên, Email)
            if (!string.IsNullOrEmpty(keyword))
            {
                // Lưu ý: Email có thể null nên cần check null trước khi Contains
                query = query.Where(u => u.Username.Contains(keyword) ||
                                         u.FullName.Contains(keyword) ||
                                         (u.Email != null && u.Email.Contains(keyword)));
            }

            // Lọc theo phân loại (Logic bạn yêu cầu)
            if (type == "customer")
            {
                query = query.Where(u => u.Role == "customer");
            }
            else // type == "employee" (Mặc định)
            {
                query = query.Where(u => u.Role == "admin" || u.Role == "staff");
            }

            int totalCount = query.Count();

            var list = query.OrderByDescending(u => u.CreatedAt) // Dùng CreatedDate theo Entity mới
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            return (list, totalCount);
        }

        // 2. Lấy chi tiết user
        public User? GetUserById(int id)
        {
            return _context.Users.Find(id);
        }

        // 3. Thêm mới hoặc Cập nhật (Upsert)
        public void SaveUser(User user)
        {
            // Validate: Kiểm tra trùng Username (trừ chính nó)
            var exists = _context.Users.Any(u => u.Username == user.Username && u.UserId != user.UserId);
            if (exists)
            {
                throw new Exception($"Tên đăng nhập '{user.Username}' đã tồn tại!");
            }

            if (user.UserId == 0)
            {
                // --- THÊM MỚI ---
                user.CreatedAt = DateTime.Now;
                // Nếu chưa nhập password thì báo lỗi (hoặc set mặc định)
                if (string.IsNullOrEmpty(user.Password))
                    throw new Exception("Mật khẩu không được để trống khi tạo mới!");

                _context.Users.Add(user);
            }
            else
            {
                // --- CẬP NHẬT ---
                var oldUser = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserId == user.UserId);
                if (oldUser == null) throw new Exception("Không tìm thấy người dùng!");

                // Logic giữ mật khẩu cũ nếu ô input để trống
                if (string.IsNullOrEmpty(user.Password))
                {
                    user.Password = oldUser.Password;
                }

                // Giữ nguyên ngày tạo cũ
                user.CreatedAt = oldUser.CreatedAt;

                _context.Users.Update(user);
            }

            _context.SaveChanges();
        }

        // 4. Xóa User
        public void DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) throw new Exception("Người dùng không tồn tại!");

            _context.Users.Remove(user);
            _context.SaveChanges();
        }
    }
}
