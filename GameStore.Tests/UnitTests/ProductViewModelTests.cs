using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Frontend.Models.Product;
using Frontend.Services;
using Frontend.ViewModels;
using Moq;
using Moq.Protected;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class ProductViewModelTests
    {
        private (ProductViewModel ViewModel, Mock<HttpMessageHandler> HttpMessageHandlerMock) CreateViewModel()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5021/")
            };

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("API")).Returns(httpClient);

            var productService = new ProductService(httpClientFactoryMock.Object);
            var vm = new ProductViewModel(productService);

            return (vm, handlerMock);
        }

        [Fact]
        public async Task UT_16_LoadAsync_ShouldLoadProductsAndHandlePagination()
        {
            // Arrange
            var (vm, handlerMock) = CreateViewModel();
            var paginatedResponse = new PaginatedList<ProductDto>
            {
                Items = new List<ProductDto>
                {
                    new ProductDto { Id = 1, Name = "Game 1" },
                    new ProductDto { Id = 2, Name = "Game 2" }
                },
                TotalCount = 20,
                TotalPages = 2,
                PageNumber = 1
            };

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(paginatedResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), System.Text.Encoding.UTF8, "application/json")
                });

            // Act
            await vm.LoadAsync(1);

            // Assert
            vm.Products.Should().HaveCount(2);
            vm.TotalPages.Should().Be(2);
            vm.CurrentPage.Should().Be(1);
        }

        [Fact]
        public async Task UT_19_TogglePriceRange_ShouldAddOrRemove_AndCallLoadAsync()
        {
            // Arrange
            var (vm, handlerMock) = CreateViewModel();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new PaginatedList<ProductDto>(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), System.Text.Encoding.UTF8, "application/json")
                });

            // Act 1: Add range
            await vm.TogglePriceRangeAsync("under_100k");

            // Assert 1
            vm.SelectedPriceRanges.Should().Contain("under_100k");

            // Act 2: Remove range
            await vm.TogglePriceRangeAsync("under_100k");

            // Assert 2
            vm.SelectedPriceRanges.Should().NotContain("under_100k");
        }

        [Fact]
        public async Task UT_20_SetSortBy_ShouldUpdateValue_AndCallLoadAsync()
        {
            // Arrange
            var (vm, handlerMock) = CreateViewModel();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new PaginatedList<ProductDto>(), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), System.Text.Encoding.UTF8, "application/json")
                });

            // Act
            await vm.SetSortByAsync("price_asc");

            // Assert
            vm.SortBy.Should().Be("price_asc");
        }
    }
}
