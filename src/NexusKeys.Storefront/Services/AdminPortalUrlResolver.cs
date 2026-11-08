using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace Frontend.Services
{
    public static class AdminPortalUrlResolver
    {
        public static string ResolveAdminPortalUrl(IConfiguration? configuration, NavigationManager? navigationManager = null)
        {
            var adminUrl = configuration?["AdminPortal:BaseUrl"] ?? configuration?["AdminPortal__BaseUrl"];

            var isLocal = false;
            if (navigationManager != null && !string.IsNullOrWhiteSpace(navigationManager.BaseUri))
            {
                try
                {
                    var uri = new Uri(navigationManager.BaseUri);
                    isLocal = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                              uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    isLocal = false;
                }
            }

            if (isLocal)
            {
                if (string.IsNullOrWhiteSpace(adminUrl) ||
                    adminUrl.Contains("nexuskeys-api", StringComparison.OrdinalIgnoreCase) ||
                    adminUrl.Contains("nexuskeys-storefront", StringComparison.OrdinalIgnoreCase))
                {
                    return "http://localhost:3001";
                }

                if (!adminUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !adminUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    adminUrl = "http://" + adminUrl;
                }
                return adminUrl.TrimEnd('/');
            }

            // In production / Cloud (Render):
            if (string.IsNullOrWhiteSpace(adminUrl) ||
                adminUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                adminUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                adminUrl.Contains("nexuskeys-api", StringComparison.OrdinalIgnoreCase) ||
                adminUrl.Contains("nexuskeys-storefront", StringComparison.OrdinalIgnoreCase))
            {
                return "https://nexuskeys-admin.onrender.com";
            }

            if (!adminUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !adminUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                adminUrl = "https://" + adminUrl;
            }

            return adminUrl.TrimEnd('/');
        }
    }
}
