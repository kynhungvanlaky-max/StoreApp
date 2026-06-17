using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;

namespace Backend.Controllers
{
    public class PaymentConfigDto
    {
        public string GatewayName { get; set; } = string.Empty;
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
    }

    public class PaymentConfigsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentConfigsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync("/api/payment-configs");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                var configs = await response.Content.ReadFromJsonAsync<List<PaymentConfigDto>>();
                return View(configs ?? new List<PaymentConfigDto>());
            }

            return View(new List<PaymentConfigDto>());
        }

        [HttpPost]
        public async Task<IActionResult> Save(List<PaymentConfigDto> configs)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            configs = configs.Where(c => !string.IsNullOrEmpty(c.GatewayName) && !string.IsNullOrEmpty(c.ConfigKey)).ToList();

            var response = await client.PostAsJsonAsync("/api/payment-configs", configs);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Lưu cấu hình thành công!";
            }
            else
            {
                TempData["Error"] = "Có lỗi xảy ra khi lưu cấu hình.";
            }

            return RedirectToAction("Index");
        }
    }
}
