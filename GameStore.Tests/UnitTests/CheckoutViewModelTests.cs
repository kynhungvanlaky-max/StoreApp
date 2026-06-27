using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Frontend.Models.Cart;
using Frontend.Models.Order;
using Frontend.Services;
using Frontend.ViewModels;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Moq.Protected;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class CheckoutViewModelTests
    {
        private (CheckoutViewModel ViewModel, CartViewModel CartVm, Mock<HttpMessageHandler> HandlerMock) CreateViewModel()
        {
            var dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
            var dataProtectorMock = new Mock<IDataProtector>();
            dataProtectionProviderMock.Setup(x => x.CreateProtector(It.IsAny<string>())).Returns(dataProtectorMock.Object);

            var jsRuntimeMock = new Mock<Microsoft.JSInterop.IJSRuntime>();
            var storage = new ProtectedLocalStorage(jsRuntimeMock.Object, dataProtectionProviderMock.Object);
            var toastService = new Frontend.Services.ToastService();
            var cartVm = new CartViewModel(storage, toastService);

            var handlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5021/")
            };

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("API")).Returns(httpClient);

            var authService = new AuthService(httpClientFactoryMock.Object);
            var authVm = new AuthViewModel(authService, storage);

            var orderService = new OrderService(httpClientFactoryMock.Object, authVm);
            var vm = new CheckoutViewModel(orderService, cartVm, new Frontend.Services.ToastService());

            return (vm, cartVm, handlerMock);
        }

        [Fact]
        public async Task UT_18_Checkout_ShouldFailWhenNoItemsSelected()
        {
            // Arrange
            var (vm, cartVm, _) = CreateViewModel();

            // Act
            var result = await vm.ProcessCheckoutAsync();

            // Assert
            result.Should().BeNull();
            vm.ErrorMessage.Should().Be("Vui lòng chọn ít nhất 1 sản phẩm để thanh toán.");
        }
    }
}
