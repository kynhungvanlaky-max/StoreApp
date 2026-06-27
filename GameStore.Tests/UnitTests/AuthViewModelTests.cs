using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Frontend.Models.Auth;
using Frontend.Services;
using Frontend.ViewModels;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Moq.Protected;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class AuthViewModelTests
    {
        private (AuthViewModel ViewModel, Mock<HttpMessageHandler> HttpMessageHandlerMock) CreateViewModel()
        {
            var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
            var dataProtectorMock = new Mock<IDataProtector>();
            dataProtectionProviderMock.Setup(x => x.CreateProtector(It.IsAny<string>())).Returns(dataProtectorMock.Object);

            var jsRuntimeMock = new Mock<Microsoft.JSInterop.IJSRuntime>();
            var storage = new ProtectedLocalStorage(jsRuntimeMock.Object, dataProtectionProviderMock.Object);

            var handlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5021/")
            };

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("API")).Returns(httpClient);

            var authService = new AuthService(httpClientFactoryMock.Object);
            var vm = new AuthViewModel(authService, storage);

            return (vm, handlerMock);
        }

        [Fact]
        public async Task UT_11_LoginAsync_ShouldSetIsLoggedIn_WhenSuccessful()
        {
            // Arrange
            var (vm, handlerMock) = CreateViewModel();
            var loginResponse = new AuthResponse
            {
                Token = "fake-token",
                RefreshToken = "fake-refresh",
                Username = "testuser",
                Role = "Customer"
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
                    Content = new StringContent(JsonSerializer.Serialize(loginResponse), System.Text.Encoding.UTF8, "application/json")
                });

            var request = new LoginRequest { Email = "test@example.com", Password = "password" };

            // Act
            var result = await vm.LoginAsync(request);

            // Assert
            result.Should().BeTrue();
            vm.IsLoggedIn.Should().BeTrue();
            vm.Username.Should().Be("testuser");
            vm.Role.Should().Be("Customer");
        }

        [Fact]
        public async Task UT_12_LoginAsync_ShouldReturnFalse_WhenFailed()
        {
            // Arrange
            var (vm, handlerMock) = CreateViewModel();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized
                });

            var request = new LoginRequest { Email = "wrong@example.com", Password = "wrong" };

            // Act
            var result = await vm.LoginAsync(request);

            // Assert
            result.Should().BeFalse();
            vm.IsLoggedIn.Should().BeFalse();
            vm.Username.Should().BeNull();
        }
    }
}
