using Frontend.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class RecentlyViewedServiceTests
    {
        private (RecentlyViewedService Service, Mock<IJSRuntime> JsMock) CreateService()
        {
            var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
            var dataProtectorMock = new Mock<IDataProtector>();
            dataProtectionProviderMock.Setup(x => x.CreateProtector(It.IsAny<string>())).Returns(dataProtectorMock.Object);

            var jsRuntimeMock = new Mock<IJSRuntime>();
            var storage = new ProtectedLocalStorage(jsRuntimeMock.Object, dataProtectionProviderMock.Object);
            var service = new RecentlyViewedService(storage);

            return (service, jsRuntimeMock);
        }

        [Fact]
        public async Task GetRecentlyViewed_WhenStorageEmpty_ReturnsEmptyList()
        {
            // Arrange
            var (service, _) = CreateService();

            // Act
            var items = await service.GetRecentlyViewedAsync();

            // Assert
            Assert.NotNull(items);
            Assert.Empty(items);
        }

        [Fact]
        public async Task AddRecentlyViewed_WhenStorageFails_ShouldNotThrow()
        {
            // Arrange
            var (service, _) = CreateService();

            // Act & Assert - should not throw when storage fails
            await service.AddRecentlyViewedAsync(1, "Product A", "/img/a.jpg", 100m);
            await service.AddRecentlyViewedAsync(2, "Product B", "/img/b.jpg", 200m);

            // Storage returns empty due to JS being unavailable, but no exception should be thrown
            var items = await service.GetRecentlyViewedAsync();
            Assert.NotNull(items);
        }

        [Fact]
        public async Task RecentlyViewedItem_ShouldHaveCorrectStructure()
        {
            // Arrange
            var item = new Frontend.Services.RecentlyViewedItem
            {
                ProductId = 42,
                Name = "Test Game",
                ImageUrl = "/img/game.jpg",
                BasePrice = 299000m,
                ViewedAt = DateTime.UtcNow
            };

            // Assert
            Assert.Equal(42, item.ProductId);
            Assert.Equal("Test Game", item.Name);
            Assert.Equal("/img/game.jpg", item.ImageUrl);
            Assert.Equal(299000m, item.BasePrice);
            Assert.True((DateTime.UtcNow - item.ViewedAt).TotalSeconds < 5);
        }
    }
}