using System.Net.Http.Json;
using Store.Shared.DTOs;

namespace StoreClient.Services
{
    public class CartService
    {
        // Danh sách chứa các món hàng
        private List<CartItem> cart = new List<CartItem>();

        // Sự kiện để báo cho các Component khác biết giỏ hàng đã thay đổi
        public event Action? OnChange;

        // Inject ToastService
        private readonly ToastService _toastService;

        private readonly HttpClient _http;

        public CartService(ToastService toastService, HttpClient http)
        {
            _toastService = toastService;
            _http = http;
        }

        // Hàm thêm vào giỏ
        public void AddToCart(ProductDTO product)
        {
            var item = cart.FirstOrDefault(x => x.ProductId == product.ProductId);
            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    StockQuantity = product.Quantity,
                    Quantity = 1
                });
            }
            else
            {
                // Nếu có rồi thì tăng số lượng (nhưng không quá tồn kho)
                if (item.Quantity < item.StockQuantity)
                {
                    item.Quantity++;
                }
            }

            NotifyStateChanged();
            _toastService.ShowToast($"Đã thêm {product.ProductName} vào giỏ hàng", ToastLevel.Success);
        }

        // Hàm xóa khỏi giỏ
        public void DeleteItem(CartItem item)
        {
            cart.Remove(item);
            NotifyStateChanged();
            _toastService.ShowToast($"Đã xóa {item.ProductName} khỏi giỏ hàng", ToastLevel.Error);
        }

        public List<CartItem> GetCartItems()
        {
            return cart;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();


        // Hàm thanh toán
        public async Task<bool> Checkout(OrderCreateDTO orderDto)
        {
            // Gán danh sách giỏ hàng hiện tại vào DTO
            orderDto.CartItems = cart;

            var response = await _http.PostAsJsonAsync("api/OrderApi", orderDto);

            if (response.IsSuccessStatusCode)
            {
                // Nếu thành công: Xóa sạch giỏ hàng
                cart.Clear();
                NotifyStateChanged();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
