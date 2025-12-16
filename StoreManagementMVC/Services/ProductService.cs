using Microsoft.EntityFrameworkCore;
using StoreManagementMVC.Data;
using Store.Shared.Entities;

namespace StoreManagementMVC.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        // Lấy một sản phẩm theo Id
        public Product? GetProductById(int id)
        {
            return _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.ProductId == id);
        }

        // Thêm một sản phẩm
        public void AddProduct(Product product)
        {
            // Nếu Barcode là rỗng hoặc khoảng trắng, ép nó về null để tránh lỗi trùng lặp trong DB
            if (string.IsNullOrWhiteSpace(product.Barcode)) product.Barcode = null;
            
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                _context.Products.Add(product);
                _context.SaveChanges(); // Lưu để lấy ID

                // Sinh mã vạch nếu cần
                if (product.Barcode == null)
                {
                    product.Barcode = "89" + product.ProductId.ToString("D11");
                    _context.Products.Update(product);
                }

                int initialQty = product.Inventory?.Quantity ?? 0;

                var inventory = new Inventory
                {
                    ProductId = product.ProductId,
                    Quantity = initialQty,
                    UpdatedAt = DateTime.Now
                };
                _context.Inventories.Add(inventory);
                _context.SaveChanges();

                transaction.Commit();
            }
            catch 
            {
                transaction.Rollback();
                throw;
            }
        }

        // Cập nhật sản phẩm
        public void UpdateProduct(Product product)
        {
            var existingProd = _context.Products
                .Include(p => p.Inventory)
                .FirstOrDefault(p => p.ProductId == product.ProductId);
            if (existingProd != null)
            {
                existingProd.ProductName = product.ProductName;
                existingProd.Price = product.Price;
                existingProd.CategoryId = product.CategoryId;
                existingProd.SupplierId = product.SupplierId;
                existingProd.Unit = product.Unit;
                existingProd.Barcode = product.Barcode;

                if (existingProd.Inventory != null && product.Inventory != null)
                {
                    existingProd.Inventory.Quantity = product.Inventory.Quantity;
                    existingProd.Inventory.UpdatedAt = DateTime.Now;
                }
                else if (existingProd.Inventory == null)
                {
                    // Trường hợp dữ liệu cũ bị thiếu Inventory, tạo mới luôn
                    var newInv = new Inventory { ProductId = product.ProductId, Quantity = product.Inventory?.Quantity ?? 0 };
                    _context.Inventories.Add(newInv);
                }

                _context.SaveChanges();
            }
        }

        // Xóa sản phẩm
        public void DeleteProduct(int id)
        {
            var product = GetProductById(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
        }

        // Lấy danh sách danh mục và nhà cung cấp
        public List<Category> GetCategories() => _context.Categories.ToList();
        public List<Supplier> GetSuppliers() => _context.Suppliers.ToList();

        // Lấy toàn bộ sản phẩm
        public List<Product> GetAllProducts()
        {
            return _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }


        // Lấy sản phẩm có phân trang
        public (List<Product> products, int totalCount) GetProductsPaging(int pageIndex, int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventory)
                .AsQueryable(); // Sắp xếp sản phẩm mới nhất lên đầu

            int totalCount = query.Count();

            var products = query.OrderByDescending(p => p.ProductId)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (products, totalCount);
        }

        // Tìm kiếm sản phẩm (cho chức năng Autocomplete)
        public List<Product> SearchProducts(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return new List<Product>();

            return _context.Products
                .Where(p => p.ProductName.Contains(keyword) ||
                            (p.Barcode != null && p.Barcode.Contains(keyword)))
                .OrderBy(p => p.ProductName)
                .Take(20) // Chỉ lấy tối đa 20 kết quả để load cho nhanh
                .ToList();
        }
    }
}
