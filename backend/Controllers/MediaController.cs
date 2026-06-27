using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    public class MediaController : BaseController
    {
        public IActionResult Index()
        {
            ViewData["Token"] = Request.Cookies["access_token"];
            return View();
        }
    }
}
