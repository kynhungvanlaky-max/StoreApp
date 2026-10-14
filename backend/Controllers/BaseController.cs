using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Controllers
{
    public abstract class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var role = (context.HttpContext.Items["AdminRole"] as string) 
                ?? Backend.Extensions.JwtRoleExtractor.ExtractRole(Request.Cookies["access_token"]) 
                ?? "Administrator";

            ViewData["AdminName"] = Request.Cookies["admin_name"] ?? "Quản trị viên";
            ViewData["AdminEmail"] = Request.Cookies["admin_email"] ?? string.Empty;
            ViewData["AdminRole"] = role;
        }

        protected bool IsAdministrator => string.Equals(ViewData["AdminRole"]?.ToString(), "Administrator", StringComparison.OrdinalIgnoreCase);

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
