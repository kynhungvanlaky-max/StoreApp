using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    using Backend.Models;

    public class OrdersController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrdersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/orders?page={page}&pageSize={pageSize}");
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<OrderViewModel>>();
                    ViewBag.CurrentPage = pagedResult?.CurrentPage ?? page;
                    ViewBag.TotalPages = pagedResult?.TotalPages ?? 1;
                    ViewBag.TotalItems = pagedResult?.TotalItems ?? 0;
                    ViewBag.PageSize = pagedResult?.PageSize ?? pageSize;
                    return View(new OrderListViewModel { Orders = pagedResult?.Data ?? new List<OrderViewModel>() });
                }
                catch (Exception ex)
                {
                    SetErrorMessage("Lỗi khi tải dữ liệu đơn hàng: " + ex.Message);
                    return View(new OrderListViewModel());
                }
            }

            return View(new OrderListViewModel());
        }

        public async Task<IActionResult> Details(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/orders/{id}");
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderViewModel>();
                return View(order);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.PutAsJsonAsync($"/api/orders/{id}/status", new { Status = status });
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Cập nhật trạng thái đơn hàng thành công!");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi cập nhật trạng thái đơn hàng.");
            }

            return RedirectToAction("Details", new { id = id });
        }
    }
}
