using System.Net.Http.Json;
using Frontend.Models.Order;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class OrderService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthViewModel _authViewModel;

        public OrderService(IHttpClientFactory httpClientFactory, AuthViewModel authViewModel)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _authViewModel = authViewModel;
        }

        private async Task SetAuthorizationHeaderAsync()
        {
            var token = await _authViewModel.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<CheckoutResponse?> CheckoutAsync(CheckoutRequest request)
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync("/api/orders/checkout", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CheckoutResponse>();
            }
            return null;
        }

        public async Task<List<OrderDto>?> GetMyOrdersAsync()
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync("/api/orders/my-orders");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<OrderDto>>();
            }
            return null;
        }
        
        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync($"/api/orders/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<OrderDto>();
            }
            return null;
        }
    }
}
