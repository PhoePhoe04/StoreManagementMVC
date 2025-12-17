using Store.Shared.Entities;
using StoreManagementMVC.Data;

namespace StoreManagementMVC.Services
{
    public class CategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách có Phân trang & Tìm kiếm
        public (List<Category> categories, int totalCount) GetCategoriesPaging(int pageIndex, int pageSize, string keyword = "")
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(c => c.CategoryName.Contains(keyword));
            }

            int totalCount = query.Count();

            // Sắp xếp theo ID giảm dần (mới nhất lên đầu)
            var list = query.OrderByDescending(c => c.CategoryId)
                            .Skip((pageIndex - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            return (list, totalCount);
        }

        // 2. Lấy chi tiết
        public Category? GetCategoryById(int id)
        {
            return _context.Categories.Find(id);
        }

        // 3. Thêm hoặc Sửa (Upsert)
        public void SaveCategory(Category category)
        {
            // Kiểm tra trùng tên (nếu cần thiết)
            var exists = _context.Categories.Any(c => c.CategoryName == category.CategoryName && c.CategoryId != category.CategoryId);
            if (exists)
            {
                throw new Exception($"Danh mục '{category.CategoryName}' đã tồn tại!");
            }

            if (category.CategoryId == 0)
            {
                _context.Categories.Add(category);
            }
            else
            {
                _context.Categories.Update(category);
            }
            _context.SaveChanges();
        }

        // 4. Xóa danh mục
        public void DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) throw new Exception("Danh mục không tồn tại!");

            // Kiểm tra xem có Sản phẩm nào đang thuộc danh mục này không?
            bool hasProducts = _context.Products.Any(p => p.CategoryId == id);
            if (hasProducts)
            {
                throw new Exception("Không thể xóa! Danh mục này đang chứa sản phẩm.");
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();
        }
    }
}
