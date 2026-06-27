using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Backend.Filters
{
    public class AdminAuthorizeFilter : IAsyncActionFilter
    {
        private readonly IConfiguration _config;

        public AdminAuthorizeFilter(IConfiguration config)
        {
            _config = config;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var enableAuth = _config.GetValue<bool>("AuthSettings:EnableAuth");
            if (!enableAuth)
            {
                await next();
                return;
            }

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            
            // Bỏ qua kiểm soát cho AuthController để cho phép login/logout
            if (controllerName == "Auth")
            {
                await next();
                return;
            }

            var token = context.HttpContext.Request.Cookies["access_token"];
            var loginUrl = _config.GetValue<string>("AuthSettings:LoginUrl") ?? "http://localhost:5205/login";

            if (string.IsNullOrEmpty(token))
            {
                context.Result = new RedirectResult(loginUrl);
                return;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                
                if (!handler.CanReadToken(token))
                {
                    context.Result = new RedirectResult(loginUrl);
                    return;
                }

                var jwtToken = handler.ReadJwtToken(token);

                // Kiểm tra xem token đã hết hạn chưa
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    context.Result = new RedirectResult(loginUrl);
                    return;
                }

                // Kiểm tra vai trò (Admin, Editor, Viewer)
                var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
                if (roleClaim == null || (roleClaim.Value != "Administrator" && roleClaim.Value != "Editor" && roleClaim.Value != "Viewer"))
                {
                    context.Result = new ContentResult
                    {
                        StatusCode = 403,
                        Content = "Bạn không có quyền truy cập trang quản trị."
                    };
                    return;
                }

                // Lưu role vào HttpContext để view có thể dùng
                context.HttpContext.Items["UserRole"] = roleClaim.Value;
                context.HttpContext.Items["Username"] = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "Admin";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auth Filter Error: {ex.Message}");
                context.Result = new RedirectResult(loginUrl);
                return;
            }

            await next();
        }
    }
}
