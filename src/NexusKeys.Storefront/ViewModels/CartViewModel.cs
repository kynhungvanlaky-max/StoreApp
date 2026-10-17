using Frontend.Models.Cart;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class CartViewModel
    {
        private readonly ProtectedLocalStorage _storage;
        private readonly ToastService _toastService;
        
        public List<CartItem> Items { get; private set; } = new();
        public decimal TotalAmount => Items.Sum(i => i.Price);
        public int TotalItems => Items.Count;
        
        public decimal SelectedTotalAmount => Items.Where(i => i.IsSelected).Sum(i => i.Price);
        public int SelectedTotalItems => Items.Count(i => i.IsSelected);
        
        public event Action? StateChanged;

        public CartViewModel(ProtectedLocalStorage storage, ToastService toastService)
        {
            _storage = storage;
            _toastService = toastService;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var result = await _storage.GetAsync<List<CartItem>>("cart_items");
                if (result.Success && result.Value != null)
                {
                    Items = result.Value;
                    StateChanged?.Invoke();
                }
            }
            catch
            {
                // Ignore storage errors
            }
        }

        public async Task AddToCartAsync(CartItem item)
        {
            if (!Items.Any(i => i.ProductId == item.ProductId))
            {
                Items.Add(item);
                await SaveCartAsync();
                _toastService.ShowSuccess($"Đã thêm {item.Name} vào giỏ hàng!");
            }
            else
            {
                _toastService.ShowWarning($"{item.Name} đã có trong giỏ hàng.");
            }
        }

        public async Task RemoveFromCartAsync(int productId)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                Items.Remove(item);
                await SaveCartAsync();
                _toastService.ShowInfo("Đã xóa sản phẩm khỏi giỏ hàng.");
            }
        }

        public async Task ClearCartAsync()
        {
            Items.Clear();
            await SaveCartAsync();
            _toastService.ShowInfo("Đã xóa toàn bộ giỏ hàng.");
        }

        public async Task ToggleSelectionAsync(int productId)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.IsSelected = !item.IsSelected;
                await SaveCartAsync();
            }
        }

        public async Task RemoveSelectedItemsAsync()
        {
            int countBefore = Items.Count;
            Items.RemoveAll(i => i.IsSelected);
            if (Items.Count < countBefore)
            {
                await SaveCartAsync();
            }
        }

        public async Task SelectOnlyAsync(int productId)
        {
            foreach (var item in Items)
            {
                item.IsSelected = item.ProductId == productId;
            }
            await SaveCartAsync();
        }

        private async Task SaveCartAsync()
        {
            try
            {
                await _storage.SetAsync("cart_items", Items);
                StateChanged?.Invoke();
            }
            catch
            {
                // Ignore
            }
        }
    }
}
