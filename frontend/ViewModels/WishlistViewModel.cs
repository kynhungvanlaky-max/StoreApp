using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frontend.Models.Wishlist;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class WishlistViewModel
    {
        private readonly WishlistService _wishlistService;
        private readonly ToastService _toastService;
        private readonly AuthViewModel _authVM;

        public List<WishlistItem> Items { get; private set; } = new();
        public bool IsLoading { get; private set; }

        public event Action? StateChanged;

        public WishlistViewModel(WishlistService wishlistService, ToastService toastService, AuthViewModel authVM)
        {
            _wishlistService = wishlistService;
            _toastService = toastService;
            _authVM = authVM;
        }

        public async Task LoadWishlistAsync()
        {
            if (!_authVM.IsLoggedIn) return;

            IsLoading = true;
            StateChanged?.Invoke();

            Items = await _wishlistService.GetWishlistAsync();

            IsLoading = false;
            StateChanged?.Invoke();
        }

        public async Task AddToWishlistAsync(int productId, string productName)
        {
            if (!_authVM.IsLoggedIn)
            {
                _toastService.ShowWarning("Vui lòng đăng nhập để thêm vào danh sách yêu thích.");
                return;
            }

            var success = await _wishlistService.AddToWishlistAsync(productId);
            if (success)
            {
                _toastService.ShowSuccess($"Đã thêm {productName} vào danh sách yêu thích!");
                await LoadWishlistAsync(); // reload state
            }
            else
            {
                _toastService.ShowError("Có lỗi xảy ra, hoặc sản phẩm đã có trong danh sách.");
            }
        }

        public async Task RemoveFromWishlistAsync(int productId)
        {
            if (!_authVM.IsLoggedIn) return;

            var success = await _wishlistService.RemoveFromWishlistAsync(productId);
            if (success)
            {
                Items.RemoveAll(i => i.ProductId == productId);
                _toastService.ShowInfo("Đã xóa khỏi danh sách yêu thích.");
                StateChanged?.Invoke();
            }
        }

        public async Task<bool> IsInWishlistAsync(int productId)
        {
            if (!_authVM.IsLoggedIn) return false;
            
            // Check in local cache first
            if (Items.Any(i => i.ProductId == productId)) return true;

            // Then check API
            return await _wishlistService.CheckWishlistAsync(productId);
        }
    }
}
