using System.Net.Http.Json;
using Frontend.Models.Order;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class OrderService
    {
        private readonly AuthorizedApiClient _apiClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderService(AuthorizedApiClient apiClient, IHttpClientFactory httpClientFactory)
        {
            _apiClient = apiClient;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<CheckoutResponse?> CheckoutAsync(CheckoutRequest request)
        {
            using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"{ApiRoutes.V1}/checkouts")
            {
                Content = JsonContent.Create(request)
            });
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CheckoutResponse>();
            }
            return null;
        }

        public async Task<List<OrderDto>?> GetMyOrdersAsync()
        {
            using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"{ApiRoutes.V1}/users/me/orders"));
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<OrderDto>>();
            }
            return null;
        }
        
        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"{ApiRoutes.V1}/orders/{id}"));
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<OrderDto>();
            }
            return null;
        }

        public async Task<CheckoutResponse?> RetryPaymentAsync(int orderId, string paymentMethod = "VNPay")
        {
            using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"{ApiRoutes.V1}/orders/{orderId}/payment-attempts")
            {
                Content = JsonContent.Create(new { PaymentMethod = paymentMethod })
            });

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CheckoutResponse>();
            }

            return null;
        }

        public async Task<(CouponApplyResponse? Coupon, string? Error)> ApplyCouponAsync(string code, decimal orderTotal)
        {
            var client = _httpClientFactory.CreateClient("API");
            using var response = await client.PostAsJsonAsync($"{ApiRoutes.V1}/coupon-validations", new { code, orderTotal });
            if (response.IsSuccessStatusCode)
            {
                return (await response.Content.ReadFromJsonAsync<CouponApplyResponse>(), null);
            }

            var error = (await response.Content.ReadAsStringAsync()).Trim('"');
            return (null, string.IsNullOrWhiteSpace(error) ? "Mã giảm giá không hợp lệ." : error);
        }
    }
}
