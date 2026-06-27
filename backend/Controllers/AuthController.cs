using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult AutoLogin(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }

            // Set the token as a cookie so BaseController can read it
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true if using HTTPS in production
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(24) // Match token expiration
            };

            Response.Cookies.Append("access_token", token, cookieOptions);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return Redirect("http://localhost:5205/login");
        }
    }
}
