using Microsoft.AspNetCore.Mvc;
using Backend.Extensions;
using Backend.Models;

namespace Backend.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var model = new DashboardViewModel();

            try
            {
                var dashboardTask = client.GetAsync("api/v1/admin/dashboard");
                var revenueTask = client.GetAsync("api/v1/admin/monthly-revenue");
                var statusTask = client.GetAsync("api/v1/admin/orders-by-status");
                var productsTask = client.GetAsync("api/v1/admin/top-products?top=5");

                await Task.WhenAll(dashboardTask, revenueTask, statusTask, productsTask);

                if (!dashboardTask.Result.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Không thể tải dữ liệu dashboard từ API.";
                    return View(model);
                }

                var dashboard = await dashboardTask.Result.Content.ReadJsonAsync<DashboardViewModel>();
                if (dashboard != null)
                {
                    model = dashboard;
                }

                model.MonthlyRevenue = revenueTask.Result.IsSuccessStatusCode
                    ? await revenueTask.Result.Content.ReadJsonAsync<List<MonthlyRevenueViewModel>>() ?? new()
                    : new();
                model.OrdersByStatus = statusTask.Result.IsSuccessStatusCode
                    ? await statusTask.Result.Content.ReadJsonAsync<List<OrderStatusViewModel>>() ?? new()
                    : new();
                model.TopProducts = productsTask.Result.IsSuccessStatusCode
                    ? await productsTask.Result.Content.ReadJsonAsync<List<TopProductViewModel>>() ?? new()
                    : new();
                model.ApiAvailable = true;
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "API hiện không khả dụng. Vui lòng kiểm tra dịch vụ API.";
            }

            return View(model);
        }
    }
}
