using System.Net.Http.Json;

namespace Frontend.Services
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string PaymentGateway { get; set; } = string.Empty;
        public string GatewayTransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class DepositRequest
    {
        public decimal Amount { get; set; }
    }

    public class WalletService
    {
        private readonly HttpClient _httpClient;

        public WalletService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public async Task<List<TransactionDto>> GetTransactionsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TransactionDto>>("/api/wallet/transactions") ?? new List<TransactionDto>();
            }
            catch
            {
                return new List<TransactionDto>();
            }
        }

        public async Task<bool> DepositAsync(decimal amount)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/wallet/deposit", new DepositRequest { Amount = amount });
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
