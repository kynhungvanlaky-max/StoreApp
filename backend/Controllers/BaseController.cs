using Microsoft.AspNetCore.Mvc;
using Backend.Services;

namespace Backend.Controllers
{
    public abstract class BaseController : Controller
    {
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Transfer role/username from HttpContext.Items (set by AdminAuthorizeFilter) to ViewData
            if (HttpContext.Items["UserRole"] is string role)
            {
                ViewData["UserRole"] = role;
            }
            if (HttpContext.Items["Username"] is string username)
            {
                ViewData["Username"] = username;
            }
        }

        protected IActionResult HandleApiResponse(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var config = HttpContext.RequestServices.GetService<IConfiguration>();
                var loginUrl = config?["AuthSettings:LoginUrl"] 
                               ?? config?["FrontendBaseUrl"] + "/login"
                               ?? "http://localhost:5205/login";
                return Redirect(loginUrl);
            }

            return null!; // Trả về null nếu không có lỗi auth, để controller con tiếp tục xử lý
        }
        
        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        protected void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }
    }
}
