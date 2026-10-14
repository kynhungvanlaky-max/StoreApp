using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAdminRoleAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = (context.HttpContext.Items["AdminRole"] as string)
                ?? Backend.Extensions.JwtRoleExtractor.ExtractRole(context.HttpContext.Request.Cookies["access_token"])
                ?? "Administrator";

            if (!string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase))
            {
                if (context.Controller is Controller controller)
                {
                    controller.TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này. Chức năng chỉ dành riêng cho Quản trị viên (Administrator).";
                }
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
