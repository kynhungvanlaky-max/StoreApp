using System.Text.Json;

namespace Backend.Extensions
{
    public static class JwtRoleExtractor
    {
        public static string? ExtractRole(string? jwtToken)
        {
            if (string.IsNullOrWhiteSpace(jwtToken)) return null;

            var parts = jwtToken.Split('.');
            if (parts.Length < 2) return null;

            try
            {
                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var jsonBytes = Convert.FromBase64String(payload);
                using var doc = JsonDocument.Parse(jsonBytes);
                var root = doc.RootElement;

                // Standard role claims
                if (root.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var claimRole))
                {
                    return claimRole.GetString();
                }

                if (root.TryGetProperty("role", out var role))
                {
                    return role.GetString();
                }

                if (root.TryGetProperty("Role", out var upperRole))
                {
                    return upperRole.GetString();
                }
            }
            catch
            {
                // Fallback for non-JWT or opaque tokens
            }

            return null;
        }
    }
}
