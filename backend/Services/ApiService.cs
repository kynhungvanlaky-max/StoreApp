using System.Text;
using System.Text.Json;
using Backend.Models;

namespace Backend.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ApiService(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;
        private HttpClient CreateClient() => _httpClientFactory.CreateClient("API");

        public async Task<ApiResult<T>> GetAsync<T>(string endpoint) where T : class
        {
            try
            {
                var res = await CreateClient().GetAsync(endpoint);
                return await HandleResponse<T>(res);
            }
            catch (Exception ex) { return ApiResult<T>.Fail($"Connection error: {ex.Message}"); }
        }

        public async Task<ApiResult<PagedResponse<T>>> GetPagedAsync<T>(string endpoint) where T : class
        {
            try
            {
                var res = await CreateClient().GetAsync(endpoint);
                if (!res.IsSuccessStatusCode)
                    return res.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden
                        ? ApiResult<PagedResponse<T>>.AuthFail() : ApiResult<PagedResponse<T>>.Fail($"API Error: {res.StatusCode}");
                var data = JsonSerializer.Deserialize<PagedResponse<T>>(await res.Content.ReadAsStringAsync(), JsonOptions);
                return ApiResult<PagedResponse<T>>.Ok(data ?? new PagedResponse<T>());
            }
            catch (Exception ex) { return ApiResult<PagedResponse<T>>.Fail($"Error: {ex.Message}"); }
        }

        public async Task<ApiResult<bool>> PostAsync<T>(string endpoint, T body)
        {
            try { return HandleSimpleResponse(await CreateClient().PostAsJsonAsync(endpoint, body)); }
            catch (Exception ex) { return ApiResult<bool>.Fail($"Connection error: {ex.Message}"); }
        }

        public async Task<ApiResult<bool>> PostAsync(string endpoint)
        {
            try { return HandleSimpleResponse(await CreateClient().PostAsync(endpoint, null)); }
            catch (Exception ex) { return ApiResult<bool>.Fail($"Connection error: {ex.Message}"); }
        }

        public async Task<ApiResult<bool>> PutAsync<T>(string endpoint, T body)
        {
            try { return HandleSimpleResponse(await CreateClient().PutAsJsonAsync(endpoint, body)); }
            catch (Exception ex) { return ApiResult<bool>.Fail($"Connection error: {ex.Message}"); }
        }

        public async Task<ApiResult<bool>> PutAsync(string endpoint)
        {
            try { return HandleSimpleResponse(await CreateClient().PutAsync(endpoint, null)); }
            catch (Exception ex) { return ApiResult<bool>.Fail($"Connection error: {ex.Message}"); }
        }

        public async Task<ApiResult<bool>> DeleteAsync(string endpoint)
        {
            try { return HandleSimpleResponse(await CreateClient().DeleteAsync(endpoint)); }
            catch (Exception ex) { return ApiResult<bool>.Fail($"Connection error: {ex.Message}"); }
        }

        private async Task<ApiResult<T>> HandleResponse<T>(HttpResponseMessage res) where T : class
        {
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
                    return ApiResult<T>.AuthFail();
                return ApiResult<T>.Fail($"API Error ({res.StatusCode}): {await res.Content.ReadAsStringAsync()}");
            }
            var data = JsonSerializer.Deserialize<T>(await res.Content.ReadAsStringAsync(), JsonOptions);
            return ApiResult<T>.Ok(data ?? Activator.CreateInstance<T>());
        }

        private static ApiResult<bool> HandleSimpleResponse(HttpResponseMessage res)
        {
            if (!res.IsSuccessStatusCode)
                return res.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden
                    ? ApiResult<bool>.AuthFail() : ApiResult<bool>.Fail($"API Error: {res.StatusCode}");
            return ApiResult<bool>.Ok(true);
        }
    }

    public class ApiResult<T>
    {
        public bool IsSuccess { get; private set; }
        public bool IsAuthFailure { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }
        public static ApiResult<T> Ok(T data) => new() { IsSuccess = true, Data = data };
        public static ApiResult<T> Fail(string msg) => new() { IsSuccess = false, ErrorMessage = msg };
        public static ApiResult<T> AuthFail() => new() { IsSuccess = false, IsAuthFailure = true, ErrorMessage = "Unauthorized" };
    }
}

