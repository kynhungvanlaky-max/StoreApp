using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Frontend.Models.Product;
using Frontend.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class RecommendationServiceTests
    {
        private (RecommendationService Service, Mock<HttpMessageHandler> HttpMessageHandlerMock) CreateService()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5021/")
            };

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("API")).Returns(httpClient);

            var service = new RecommendationService(httpClientFactoryMock.Object);
            return (service, handlerMock);
        }

        [Fact]
        public async Task GetRecommendations_ReturnsEmptyList_WhenApiFails()
        {
            // Arrange
            var (service, handlerMock) = CreateService();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await service.GetRecommendationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRecommendations_ReturnsProductList_WhenApiSucceeds()
        {
            // Arrange
            var (service, handlerMock) = CreateService();
            var products = new List<ProductDto>
            {
                new() { Id = 1, Name = "Game A", BasePrice = 100m, ImageUrl = "/img/a.jpg" },
                new() { Id = 2, Name = "Game B", BasePrice = 200m, ImageUrl = "/img/b.jpg" }
            };

            var json = JsonSerializer.Serialize(products);
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                });

            // Act
            var result = await service.GetRecommendationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Game A", result[0].Name);
        }

        [Fact]
        public async Task GetRecommendations_PassesQueryParameters()
        {
            // Arrange
            var (service, handlerMock) = CreateService();
            HttpRequestMessage? capturedRequest = null;

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("[]") })
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req);

            // Act
            await service.GetRecommendationsAsync(categoryId: 3, excludeProductId: 5, count: 4);

            // Assert
            Assert.NotNull(capturedRequest);
            var url = capturedRequest.RequestUri?.ToString();
            Assert.Contains("count=4", url);
            Assert.Contains("categoryId=3", url);
            Assert.Contains("excludeProductId=5", url);
        }

        [Fact]
        public async Task GetTrending_ReturnsEmptyList_WhenApiFails()
        {
            // Arrange
            var (service, handlerMock) = CreateService();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await service.GetTrendingAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTrending_ReturnsProductList_WhenApiSucceeds()
        {
            // Arrange
            var (service, handlerMock) = CreateService();
            var products = new List<ProductDto>
            {
                new() { Id = 1, Name = "Trending Game", BasePrice = 150m, ImageUrl = "/img/trending.jpg" }
            };

            var json = JsonSerializer.Serialize(products);
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                });

            // Act
            var result = await service.GetTrendingAsync(3);

            // Assert
            Assert.Single(result);
            Assert.Equal("Trending Game", result[0].Name);
        }

        [Fact]
        public async Task GetTrending_PassesCountParameter()
        {
            // Arrange
            var (service, handlerMock) = CreateService();
            HttpRequestMessage? capturedRequest = null;

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("[]") })
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req);

            // Act
            await service.GetTrendingAsync(10);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Contains("count=10", capturedRequest.RequestUri?.ToString());
        }
    }
}