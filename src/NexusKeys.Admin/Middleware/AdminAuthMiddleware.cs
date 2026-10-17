namespace Backend.Middleware
{
    public class AdminAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IHttpClientFactory httpClientFactory)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Cho phép truy cập công khai vào Auth endpoints và static files
            if (path.StartsWith("/auth/login") ||
                path.StartsWith("/auth/bridge") ||
                path.StartsWith("/auth/logout") || 
                path.StartsWith("/css") || 
                path.StartsWith("/js") || 
                path.StartsWith("/lib") || 
                path.StartsWith("/favicon.ico") ||
                path.StartsWith("/api-proxy"))
            {
                await _next(context);
                return;
            }

            // Kiểm tra Cookie access_token của người dùng
            var token = context.Request.Cookies["access_token"];

            if (string.IsNullOrEmpty(token))
            {
                // Người ngoài chưa đăng nhập -> Chuyển hướng sang trang đăng nhập ngay lập tức
                context.Response.Redirect("/Auth/Login");
                return;
            }

            var role = Backend.Extensions.JwtRoleExtractor.ExtractRole(token) ?? "Administrator";
            context.Items["AdminToken"] = token;
            context.Items["AdminRole"] = role;

            try
            {
                var client = httpClientFactory.CreateClient("API");
                using var validationResponse = await client.GetAsync("api/v1/admin/dashboard", context.RequestAborted);
                if (!validationResponse.IsSuccessStatusCode)
                {
                    context.Response.Cookies.Delete("access_token");
                    context.Response.Cookies.Delete("admin_name");
                    context.Response.Cookies.Delete("admin_email");
                    context.Response.Redirect("/Auth/Login?expired=true");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                context.Response.Redirect("/Auth/Login?apiUnavailable=true");
                return;
            }

            context.Items["ApiOnline"] = true;
            await _next(context);
        }
    }
}
