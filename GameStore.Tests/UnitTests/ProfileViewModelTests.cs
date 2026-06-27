using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Frontend.Models.User;
using Frontend.Services;
using Frontend.ViewModels;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Moq.Protected;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class ProfileViewModelTests
    {
        private (ProfileViewModel ViewModel, Mock<HttpMessageHandler> HandlerMock) CreateViewModel()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5021/")
            };

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("API")).Returns(httpClient);

            var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
            var dataProtectorMock = new Mock<IDataProtector>();
            dataProtectionProviderMock.Setup(x => x.CreateProtector(It.IsAny<string>())).Returns(dataProtectorMock.Object);

            var jsRuntimeMock = new Mock<Microsoft.JSInterop.IJSRuntime>();
            var storage = new ProtectedLocalStorage(jsRuntimeMock.Object, dataProtectionProviderMock.Object);

            var authService = new AuthService(httpClientFactoryMock.Object);
            var authVm = new AuthViewModel(authService, storage);

            var userService = new UserService(httpClientFactoryMock.Object, authVm);
            var vm = new ProfileViewModel(userService);

            return (vm, handlerMock);
        }

        [Fact]
        public async Task UT_18_UpdateProfile_ShouldSetSuccessMessage_WhenSuccessful()
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
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
                });

            // Act
            await vm.UpdateProfileAsync("New Name", "0123456789", "new-avatar.png");

            // Assert
            vm.SuccessMessage.Should().Be("Cập nhật hồ sơ thành công!");
            vm.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task UT_21_LoadLibrary_ShouldPopulateLibraryWithKeyCodes()
        {
            // Arrange
            var (vm, handlerMock) = CreateViewModel();

            var libraryItems = new System.Collections.Generic.List<LibraryItemDto>
            {
                new LibraryItemDto { Id = 1, Name = "Game 1", KeyCode = "ABC-DEF-GHI" }
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
                    Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(libraryItems, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }), System.Text.Encoding.UTF8, "application/json")
                });

            // Act
            await vm.LoadLibraryAsync();

            // Assert
            vm.Library.Should().HaveCount(1);
            vm.Library[0].KeyCode.Should().Be("ABC-DEF-GHI");
        }
    }
}
