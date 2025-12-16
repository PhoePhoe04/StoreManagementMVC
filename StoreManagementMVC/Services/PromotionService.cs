using StoreManagementMVC.Data;
using Store.Shared.Entities;

namespace StoreManagementMVC.Services
{
    public class PromotionService
    {
        private readonly AppDbContext _context;

        public PromotionService(AppDbContext context) => _context = context;

        // Lấy danh sách phân trang
        public (List<Promotion> promotions, int totalCount) GetPromotionsPaging(int pageIndex, int pageSize, string keyword = "")
        {
            var query = _context.Promotions.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.PromoCode.Contains(keyword) || p.Description.Contains(keyword));
            }

            int totalCount = query.Count();

            // Sắp xếp: Cái nào đang chạy (active) đưa lên đầu, sau đó đến ngày tạo mới nhất
            var list = query.OrderByDescending(p => p.Status == "active")
                            .ThenByDescending(p => p.PromoId)
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            return (list, totalCount);
        }

        // Lấy chi tiết
        public Promotion? GetPromotionById(int id) => _context.Promotions.Find(id);

        // Thêm mới
        public void AddPromotion(Promotion promo)
        {
            // Kiểm tra trùng mã code
            if (_context.Promotions.Any(p => p.PromoCode == promo.PromoCode))
            {
                throw new Exception($"Mã khuyến mãi '{promo.PromoCode}' đã tồn tại!");
            }

            promo.UsedCount = 0; // Mặc định chưa dùng lần nào
            _context.Promotions.Add(promo);
            _context.SaveChanges();
        }

        // Cập nhật
        public void UpdatePromotion(Promotion promo)
        {
            // Kiểm tra trùng mã (nếu sửa mã code)
            var exists = _context.Promotions.Any(p => p.PromoCode == promo.PromoCode && p.PromoId != promo.PromoId);
            if (exists)
            {
                throw new Exception($"Mã khuyến mãi '{promo.PromoCode}' đã tồn tại!");
            }

            _context.Promotions.Update(promo);
            _context.SaveChanges();
        }

        // Xóa
        public void DeletePromotion(int id)
        {
            var promo = _context.Promotions.Find(id);
            if (promo != null)
            {
                // Kiểm tra xem mã này đã được dùng trong đơn hàng nào chưa
                // (Cần check bảng Orders, nhưng hiện tại ta cho xóa thoải mái hoặc soft delete)
                _context.Promotions.Remove(promo);
                _context.SaveChanges();
            }
        }

        // Kiểm tra mã khuyến mãi hợp lệ
        public Promotion? CheckValidPromotion(string code, decimal orderTotal)
        {
            var promo = _context.Promotions.FirstOrDefault(p => p.PromoCode == code && p.Status == "active");

            if (promo == null) throw new Exception("Mã khuyến mãi không tồn tại hoặc đã bị khóa.");

            if (DateTime.Now < promo.StartDate || DateTime.Now > promo.EndDate)
                throw new Exception("Mã này chưa bắt đầu hoặc đã hết hạn.");

            if (promo.UsageLimit > 0 && promo.UsedCount >= promo.UsageLimit)
                throw new Exception("Mã này đã hết lượt sử dụng.");

            if (orderTotal < promo.MinOrderAmount)
                throw new Exception($"Đơn hàng phải từ {promo.MinOrderAmount:N0}đ mới được dùng mã này.");

            return promo;
        }
    }
}
