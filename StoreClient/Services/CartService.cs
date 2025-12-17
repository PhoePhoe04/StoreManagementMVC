using Microsoft.JSInterop;
using System.Text.Json;
using Store.Shared.DTOs;

namespace StoreClient.Services
{
    public class CartService
    {
        private readonly IJSRuntime _js;
        // Event để báo cho Navbar cập nhật số lượng (nếu có)
        public event Action OnChange;

        public CartService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task AddToCart(CartItemDTO item)
        {
            // 1. Lấy giỏ hàng cũ ra
            var cart = await GetCartItems();

            // 2. Kiểm tra xem sản phẩm đã có chưa
            var existingItem = cart.FirstOrDefault(x => x.ProductId == item.ProductId);
            if (existingItem != null)
            {
                // Có rồi thì cộng dồn số lượng
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                // Chưa có thì thêm mới
                cart.Add(item);
            }

            // 3. Lưu lại vào LocalStorage (Phải chuyển sang chuỗi JSON)
            var json = JsonSerializer.Serialize(cart);
            await _js.InvokeVoidAsync("localStorage.setItem", "cart", json);

            // 4. Báo hiệu thay đổi giao diện
            OnChange?.Invoke();
        }

        public async Task<List<CartItemDTO>> GetCartItems()
        {
            // Lấy chuỗi JSON từ LocalStorage
            var json = await _js.InvokeAsync<string>("localStorage.getItem", "cart");

            if (string.IsNullOrEmpty(json))
            {
                return new List<CartItemDTO>();
            }

            try
            {
                // Chuyển ngược từ JSON sang List
                return JsonSerializer.Deserialize<List<CartItemDTO>>(json) ?? new List<CartItemDTO>();
            }
            catch
            {
                return new List<CartItemDTO>();
            }
        }

        public async Task DeleteItem(CartItemDTO item)
        {
            var cart = await GetCartItems();
            var itemToRemove = cart.FirstOrDefault(x => x.ProductId == item.ProductId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                await SaveCart(cart);
            }
        }

        // Hàm lưu đè danh sách (Dùng khi tăng giảm số lượng)
        public async Task SaveCart(List<CartItemDTO> cart)
        {
            var json = JsonSerializer.Serialize(cart);
            await _js.InvokeVoidAsync("localStorage.setItem", "cart", json);
            OnChange?.Invoke(); // Báo UI vẽ lại
        }

        public async Task ClearCart()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "cart");
            OnChange?.Invoke();
        }
    }
}