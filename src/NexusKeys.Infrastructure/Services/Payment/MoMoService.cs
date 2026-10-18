using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using GameStore.API.Data;

namespace GameStore.API.Services.Payment
{
    public class MoMoService
    {
        private readonly GameStoreDbContext _db;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public MoMoService(GameStoreDbContext db, IConfiguration config, HttpClient httpClient)
        {
            _db = db;
            _config = config;
            _httpClient = httpClient;
        }

        private string GetConfig(string key, string fallbackConfigKey)
        {
            var config = _db.PaymentGatewayConfigs.FirstOrDefault(c => c.GatewayName == "MoMo" && c.ConfigKey == key);
            return config != null ? config.ConfigValue : _config[fallbackConfigKey]!;
        }

        public async Task<string> CreatePaymentUrlAsync(int orderId, decimal totalAmount, string orderInfo)
        {
            var partnerCode = GetConfig("PartnerCode", "MoMo:PartnerCode");
            var accessKey = GetConfig("AccessKey", "MoMo:AccessKey");
            var secretKey = GetConfig("SecretKey", "MoMo:SecretKey");
            var endpoint = GetConfig("Endpoint", "MoMo:Endpoint");
            var returnUrl = GetConfig("ReturnUrl", "MoMo:ReturnUrl");
            var notifyUrl = GetConfig("NotifyUrl", "MoMo:NotifyUrl");

            var amount = ((long)totalAmount).ToString();
            var orderIdStr = orderId.ToString() + "_" + DateTime.UtcNow.Ticks;
            var requestId = Guid.NewGuid().ToString();
            var extraData = "";

            // rawHash
            var rawHash = "accessKey=" + accessKey +
                          "&amount=" + amount +
                          "&extraData=" + extraData +
                          "&ipnUrl=" + notifyUrl +
                          "&orderId=" + orderIdStr +
                          "&orderInfo=" + orderInfo +
                          "&partnerCode=" + partnerCode +
                          "&redirectUrl=" + returnUrl +
                          "&requestId=" + requestId +
                          "&requestType=captureWallet";

            var signature = HmacSHA256(secretKey, rawHash);

            var requestData = new
            {
                partnerCode = partnerCode,
                requestId = requestId,
                amount = amount,
                orderId = orderIdStr,
                orderInfo = orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                requestType = "captureWallet",
                extraData = extraData,
                lang = "vi",
                signature = signature
            };

            var jsonRequest = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                using var document = JsonDocument.Parse(responseString);
                if (document.RootElement.TryGetProperty("payUrl", out var payUrlElement))
                {
                    return payUrlElement.GetString() ?? string.Empty;
                }
                
                // If error, return an empty string or log error
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool ValidateSignature(string partnerCode, string orderId, string requestId, string amount, string orderInfo, string orderType, string transId, string message, string localMessage, string responseTime, string errorCode, string payType, string extraData, string signature)
        {
            var accessKey = GetConfig("AccessKey", "MoMo:AccessKey");
            var secretKey = GetConfig("SecretKey", "MoMo:SecretKey");

            var rawHash = "accessKey=" + accessKey +
                          "&amount=" + amount +
                          "&extraData=" + extraData +
                          "&message=" + message +
                          "&orderId=" + orderId +
                          "&orderInfo=" + orderInfo +
                          "&orderType=" + orderType +
                          "&partnerCode=" + partnerCode +
                          "&payType=" + payType +
                          "&requestId=" + requestId +
                          "&responseTime=" + responseTime +
                          "&resultCode=" + errorCode +
                          "&transId=" + transId;

            var checkHash = HmacSHA256(secretKey, rawHash);
            var expectedBytes = Encoding.UTF8.GetBytes(checkHash.ToLowerInvariant());
            var actualBytes = Encoding.UTF8.GetBytes((signature ?? string.Empty).ToLowerInvariant());
            return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }

        private string HmacSHA256(string key, string inputData)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmacsha256 = new HMACSHA256(keyBytes))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
            }
        }
    }
}
