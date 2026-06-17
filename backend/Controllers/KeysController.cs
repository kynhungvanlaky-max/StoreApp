using Microsoft.AspNetCore.Mvc;

namespace Backend.Models
{
    public class ProductKeyViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string KeyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; 
        public bool IsSold { get; set; }
        public int ProductId { get; set; }
        public int? OrderId { get; set; }
    }


}

namespace Backend.Controllers
{
    using Backend.Models;
    using System.Text.Json;
    using System.Text;

    public class KeysController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public KeysController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync("/api/keys");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            var viewModel = new KeyListViewModel();

            if (response.IsSuccessStatusCode)
            {
                var keys = await response.Content.ReadFromJsonAsync<List<ProductKeyViewModel>>();
                if (keys != null)
                {
                    foreach (var k in keys)
                    {
                        k.Status = k.IsSold ? "Đã bán" : "Chưa bán";
                    }
                    viewModel.Keys = keys;
                }
            }

            var prodResponse = await client.GetAsync("/api/products?pageSize=1000");
            if (prodResponse.IsSuccessStatusCode)
            {
                var pagedProds = await prodResponse.Content.ReadFromJsonAsync<PagedResponse<ProductDto>>();
                viewModel.Products = pagedProds?.Data ?? new List<ProductDto>();
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Generate(int productId, int quantity)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            int successCount = 0;
            for (int i = 0; i < quantity; i++)
            {
                string randomKey = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
                string formattedKey = $"{randomKey.Substring(0,4)}-{randomKey.Substring(4,4)}-{randomKey.Substring(8,4)}-{randomKey.Substring(12,4)}";

                var jsonContent = new StringContent(JsonSerializer.Serialize(new { ProductId = productId, KeyCode = formattedKey, IsSold = false }), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/api/keys", jsonContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return Redirect("http://localhost:3000/Auth/Login");
                }

                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                }
            }

            TempData["SuccessMessage"] = $"Đã tạo thành công {successCount}/{quantity} key mới.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.DeleteAsync($"/api/keys/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đã xóa mã game thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa mã game.";
            }

            return RedirectToAction("Index");
        }
    }
}
