using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Frontend.Models.Cart;
using Frontend.ViewModels;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class CartViewModelTests
    {
        private CartViewModel CreateViewModel()
        {
            // We can mock ProtectedLocalStorage by providing a mocked IDataProtectionProvider
            var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
            var dataProtectorMock = new Mock<IDataProtector>();
            dataProtectionProviderMock.Setup(x => x.CreateProtector(It.IsAny<string>())).Returns(dataProtectorMock.Object);

            var jsRuntimeMock = new Mock<Microsoft.JSInterop.IJSRuntime>();
            var storage = new ProtectedLocalStorage(jsRuntimeMock.Object, dataProtectionProviderMock.Object);
            var toastService = new Frontend.Services.ToastService();
            return new CartViewModel(storage, toastService);
        }

        [Fact]
        public async Task UT_13_AddToCart_ShouldIncreaseItemCount()
        {
            // Arrange
            var vm = CreateViewModel();
            var item = new CartItem { ProductId = 1, Name = "Test Game", Price = 500000 };

            // Act
            await vm.AddToCartAsync(item);

            // Assert
            vm.TotalItems.Should().Be(1);
            vm.Items.First().ProductId.Should().Be(1);
        }

        [Fact]
        public async Task UT_14_RemoveFromCart_ShouldDecreaseItemCount()
        {
            // Arrange
            var vm = CreateViewModel();
            var item = new CartItem { ProductId = 1, Name = "Test Game", Price = 500000 };
            await vm.AddToCartAsync(item);

            // Act
            await vm.RemoveFromCartAsync(1);

            // Assert
            vm.TotalItems.Should().Be(0);
            vm.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task UT_15_CalculateTotalAmount_ShouldSumPrices()
        {
            // Arrange
            var vm = CreateViewModel();
            await vm.AddToCartAsync(new CartItem { ProductId = 1, Price = 100000 });
            await vm.AddToCartAsync(new CartItem { ProductId = 2, Price = 200000 });
            await vm.AddToCartAsync(new CartItem { ProductId = 3, Price = 300000 });

            // Act
            var total = vm.TotalAmount;

            // Assert
            total.Should().Be(600000);
        }

        [Fact]
        public async Task UT_16_ToggleSelection_ShouldUpdateSelectedTotalAmount()
        {
            // Arrange
            var vm = CreateViewModel();
            await vm.AddToCartAsync(new CartItem { ProductId = 1, Price = 100000, IsSelected = true });
            await vm.AddToCartAsync(new CartItem { ProductId = 2, Price = 200000, IsSelected = true });

            // Act
            await vm.ToggleSelectionAsync(1); // Deselect item 1

            // Assert
            vm.SelectedTotalAmount.Should().Be(200000);
            vm.SelectedTotalItems.Should().Be(1);
        }

        [Fact]
        public async Task UT_17_RemoveSelectedItems_ShouldOnlyRemoveSelected()
        {
            // Arrange
            var vm = CreateViewModel();
            await vm.AddToCartAsync(new CartItem { ProductId = 1, IsSelected = true });
            await vm.AddToCartAsync(new CartItem { ProductId = 2, IsSelected = false });
            await vm.AddToCartAsync(new CartItem { ProductId = 3, IsSelected = true });

            // Act
            await vm.RemoveSelectedItemsAsync();

            // Assert
            vm.TotalItems.Should().Be(1);
            vm.Items.First().ProductId.Should().Be(2);
        }
    }
}
