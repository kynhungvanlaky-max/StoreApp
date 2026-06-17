using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Models
{
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public List<string> Items { get; set; } = new();
        public List<OrderKeyDto> Keys { get; set; } = new();
    }

    public class OrderKeyDto
    {
        public int Id { get; set; }
        public string KeyCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
    }
}

namespace Backend.Controllers
{
    using Backend.Models;

    public class OrdersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrdersController(IHttpClientFactory httpClientFactory)
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

            var response = await client.GetAsync("/api/orders");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<OrderViewModel>>();
                return View(new OrderListViewModel { Orders = orders ?? new List<OrderViewModel>() });
            }

            return View(new OrderListViewModel());
        }

        public async Task<IActionResult> Details(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync($"/api/orders/{id}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderViewModel>();
                return View(order);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteKey(int orderId, int keyId)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.DeleteAsync($"/api/orders/{orderId}/keys/{keyId}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đã xóa mã game khỏi đơn hàng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa mã game.";
            }

            return RedirectToAction("Details", new { id = orderId });
        }
    }
}
