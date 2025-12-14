using StoreManagementMVC.Data;
using Store.Shared;

namespace StoreManagementMVC.Services;

public class SupplierService
{
    private readonly AppDbContext _context;

    public SupplierService(AppDbContext context)
    {
        _context = context;
    }

    // Lấy danh sách phân trang + Tìm kiếm
    public (List<Supplier> suppliers, int totalCount) GetSuppliersPaging(int pageIndex, int pageSize, string keyword = "")
    {
        var query = _context.Suppliers.AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
        {
            // Tìm theo Tên hoặc SĐT hoặc Email
            query = query.Where(s => s.Name.Contains(keyword) ||
                                     s.Phone.Contains(keyword) ||
                                     s.Email.Contains(keyword));
        }

        int totalCount = query.Count();

        var list = query.OrderByDescending(s => s.SupplierId)
                        .Skip((pageIndex - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

        return (list, totalCount);
    }

    // Lấy chi tiết
    public Supplier? GetSupplierById(int id)
    {
        return _context.Suppliers.Find(id);
    }

    // Thêm mới
    public void AddSupplier(Supplier supplier)
    {
        _context.Suppliers.Add(supplier);
        _context.SaveChanges();
    }

    // Cập nhật
    public void UpdateSupplier(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        _context.SaveChanges();
    }

    // Xóa
    public void DeleteSupplier(int id)
    {
        var supplier = _context.Suppliers.Find(id);
        if (supplier != null)
        {
            _context.Suppliers.Remove(supplier);
            _context.SaveChanges();
        }
    }
}