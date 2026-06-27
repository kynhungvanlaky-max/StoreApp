using Frontend.Models.Wishlist;
using Frontend.Services;
using Frontend.ViewModels;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http;

namespace GameStore.Tests.UnitTests
{
    public class WishlistViewModelTests
    {
        private readonly Mock<IHttpClientFactory> _mockFactory;
        private readonly ToastService _toastService;
        private readonly AuthViewModel _authVM;
        private readonly WishlistService _wishlistService;

        public WishlistViewModelTests()
        {
            _mockFactory = new Mock<IHttpClientFactory>();
            _mockFactory.Setup(x => x.CreateClient("API")).Returns(new HttpClient { BaseAddress = new System.Uri("http://localhost") });

            var jsRuntimeMock = new Mock<Microsoft.JSInterop.IJSRuntime>();
            var dataProtectionProviderMock = new Mock<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
            var mockStorage = new ProtectedLocalStorage(jsRuntimeMock.Object, dataProtectionProviderMock.Object);
            _authVM = new AuthViewModel(null!, mockStorage);
            // Will test methods considering IsLoggedIn status directly by reflection if needed


            _toastService = new ToastService();
            _wishlistService = new WishlistService(_mockFactory.Object, _authVM);
        }

        [Fact]
        public async Task AddToWishlist_WhenNotLoggedIn_ShouldShowWarning()
        {
            // Arrange
            var jsRuntimeMock = new Mock<Microsoft.JSInterop.IJSRuntime>();
            var dataProtectionProviderMock = new Mock<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
            var mockStorage = new ProtectedLocalStorage(jsRuntimeMock.Object, dataProtectionProviderMock.Object);
            var unauthVM = new AuthViewModel(null!, mockStorage);
            var wishlistService = new WishlistService(_mockFactory.Object, unauthVM);
            var viewModel = new WishlistViewModel(wishlistService, _toastService, unauthVM);

            // Act
            await viewModel.AddToWishlistAsync(1, "Game 1");

            // Assert
            var toasts = _toastService.GetToasts();
            Assert.Single(toasts);
            Assert.Equal(ToastLevel.Warning, toasts[0].Level);
            Assert.Empty(viewModel.Items);
        }

        [Fact]
        public async Task IsInWishlist_WhenInLocalCache_ShouldReturnTrue()
        {
            // Arrange
            // Hack to set IsLoggedIn = true for testing
            var isLoggedProp = typeof(AuthViewModel).GetProperty("IsLoggedIn");
            isLoggedProp?.SetValue(_authVM, true);

            var viewModel = new WishlistViewModel(_wishlistService, _toastService, _authVM);
            // Hack to inject item directly since Items setter is private
            var propertyInfo = typeof(WishlistViewModel).GetProperty("Items");
            propertyInfo?.SetValue(viewModel, new List<WishlistItem> { new WishlistItem { ProductId = 1 } });

            // Act
            var result = await viewModel.IsInWishlistAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task RemoveFromWishlist_WhenNotLoggedIn_ShouldDoNothing()
        {
            // Arrange
            var jsRuntimeMock = new Mock<Microsoft.JSInterop.IJSRuntime>();
            var dataProtectionProviderMock = new Mock<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
            var mockStorage = new ProtectedLocalStorage(jsRuntimeMock.Object, dataProtectionProviderMock.Object);
            var unauthVM = new AuthViewModel(null!, mockStorage);
            var wishlistService = new WishlistService(_mockFactory.Object, unauthVM);
            var viewModel = new WishlistViewModel(wishlistService, _toastService, unauthVM);

            // Act
            await viewModel.RemoveFromWishlistAsync(1);

            // Assert
            Assert.Empty(_toastService.GetToasts());
        }
    }
}
