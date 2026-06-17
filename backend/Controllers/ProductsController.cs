using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Backend.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DeveloperName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}

namespace Backend.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
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

            var token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient("API");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            
            var jsonContent = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync("/api/products", jsonContent);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Thêm mới sản phẩm thành công!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Lỗi khi lưu sản phẩm vào API.");
            return View(model);
        }
    }
}
