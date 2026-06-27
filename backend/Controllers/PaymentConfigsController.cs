using Microsoft.AspNetCore.Mvc;
using Backend.Models;

namespace Backend.Controllers
{
    public class PaymentConfigsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentConfigsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/payment-configs?page={page}&pageSize={pageSize}");
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = await response.Content.ReadFromJsonAsync<List<PaymentConfigDto>>();
                    ViewBag.CurrentPage = page;
                    ViewBag.TotalPages = 1;
                    ViewBag.TotalItems = result?.Count ?? 0;
                    ViewBag.PageSize = pageSize;
                    return View(result ?? new List<PaymentConfigDto>());
                }
                catch (Exception ex)
                {
                    SetErrorMessage("Lỗi khi tải dữ liệu cổng thanh toán: " + ex.Message);
                    return View(new List<PaymentConfigDto>());
                }
            }

            return View(new List<PaymentConfigDto>());
        }

        [HttpPost]
        public async Task<IActionResult> Save(List<PaymentConfigDto> configs)
        {
            var client = _httpClientFactory.CreateClient("API");
            configs = configs.Where(c => !string.IsNullOrEmpty(c.GatewayName) && !string.IsNullOrEmpty(c.ConfigKey)).ToList();

            var response = await client.PostAsJsonAsync("/api/payment-configs", configs);
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Lưu cấu hình thành công!");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi lưu cấu hình.");
            }

            return RedirectToAction("Index");
        }
    }
}
