using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    public class ProductsController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
