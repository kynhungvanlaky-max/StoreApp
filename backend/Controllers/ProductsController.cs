using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Backend.Controllers
{
    public class ProductsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            ViewData["Token"] = Request.Cookies["access_token"];
            return View();
        }

        public IActionResult Create()
        {
            return View(new Backend.Models.ProductViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Backend.Models.ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("API");
            var jsonContent = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/products", jsonContent);

            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Thêm mới sản phẩm thành công!");
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Lỗi khi lưu sản phẩm vào API.");
            return View(model);
        }
    }
}
