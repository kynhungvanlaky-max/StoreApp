using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Net;

using GameStore.API.Data;

namespace GameStore.API.Services.Payment
{
    public class VnPayService
    {
        private readonly GameStoreDbContext _db;
        private readonly IConfiguration _config;

        public VnPayService(GameStoreDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        private string GetConfig(string key, string fallbackConfigKey)
        {
            var config = _db.PaymentGatewayConfigs.FirstOrDefault(c => c.GatewayName == "VNPay" && c.ConfigKey == key);
            return config != null ? config.ConfigValue : _config[fallbackConfigKey]!;
        }

        public string CreatePaymentUrl(int orderId, decimal totalAmount, string ipAddress)
        {
            var tmnCode = GetConfig("TmnCode", "VNPay:TmnCode");
            var hashSecret = GetConfig("HashSecret", "VNPay:HashSecret");
            var baseUrl = GetConfig("BaseUrl", "VNPay:BaseUrl");
            var returnUrl = GetConfig("ReturnUrl", "VNPay:ReturnUrl");

            var vnp_Params = new SortedList<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", ((long)(totalAmount * 100)).ToString() }, // Amount * 100 (VND)
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", "Thanh toan don hang " + orderId },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_TxnRef", orderId.ToString() }
            };

            // Build query string
            var query = new StringBuilder();
            foreach (var kv in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            var queryString = query.ToString();
            if (queryString.Length > 0)
            {
                queryString = queryString.Substring(0, queryString.Length - 1);
            }

            var signData = queryString;
            var vnp_SecureHash = HmacSHA512(hashSecret, signData);
            
            var paymentUrl = baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;
            return paymentUrl;
        }

        public bool ValidateSignature(IQueryCollection queryData)
        {
            var hashSecret = GetConfig("HashSecret", "VNPay:HashSecret");
            var vnp_SecureHash = queryData["vnp_SecureHash"].ToString();

            var vnp_Params = new SortedList<string, string>();
            foreach (var key in queryData.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnp_Params.Add(key, queryData[key].ToString());
                }
            }

            if (vnp_Params.ContainsKey("vnp_SecureHashType"))
            {
                vnp_Params.Remove("vnp_SecureHashType");
            }
            if (vnp_Params.ContainsKey("vnp_SecureHash"))
            {
                vnp_Params.Remove("vnp_SecureHash");
            }

            var query = new StringBuilder();
            foreach (var kv in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            var queryString = query.ToString();
            if (queryString.Length > 0)
            {
                queryString = queryString.Substring(0, queryString.Length - 1);
            }

            var checkHash = HmacSHA512(hashSecret, queryString);
            var expectedBytes = Encoding.UTF8.GetBytes(checkHash.ToLowerInvariant());
            var actualBytes = Encoding.UTF8.GetBytes((vnp_SecureHash ?? string.Empty).ToLowerInvariant());
            return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}
