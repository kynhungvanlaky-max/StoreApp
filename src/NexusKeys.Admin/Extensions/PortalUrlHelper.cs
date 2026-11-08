using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Backend.Extensions
{
    public static class PortalUrlHelper
    {
        public static string GetFrontendUrl(IConfiguration? configuration, HttpContext? httpContext = null)
        {
            var frontendBase = configuration?["AppSettings:FrontendBaseUrl"] ?? configuration?["AppSettings__FrontendBaseUrl"];

            var isLocal = false;
            if (httpContext != null && httpContext.Request.Host.HasValue)
            {
                var host = httpContext.Request.Host.Host;
                isLocal = host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                          host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(frontendBase) ||
                (!isLocal && (frontendBase.Contains("localhost", StringComparison.OrdinalIgnoreCase) || frontendBase.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))) ||
                frontendBase.Contains("nexuskeys-api.onrender.com", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(frontendBase.Trim(), "nexuskeys-api", StringComparison.OrdinalIgnoreCase))
            {
                if (isLocal)
                {
                    return "http://localhost:5205";
                }
                return "https://nexuskeys-storefront.onrender.com";
            }

            if (!frontendBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !frontendBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                frontendBase = "https://" + frontendBase;
            }

            return frontendBase.TrimEnd('/');
        }
    }
}
