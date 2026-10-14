using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;

namespace Backend.Controllers
{
    public class OrdersController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrdersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            var client = _httpClientFactory.CreateClient("API");
            var url = $"/api/v1/orders?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";

            var orders = new List<OrderViewModel>();
            int totalPages = 1;
            int totalItems = 0;

            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var paged = await response.Content.ReadJsonAsync<PagedResponse<OrderViewModel>>();
                    if (paged != null)
                    {
                        orders = paged.Data;
                        totalPages = paged.TotalPages > 0 ? paged.TotalPages : 1;
                        totalItems = paged.TotalItems;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Order Error: " + ex.Message);
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            ViewData["OrderId"] = id;
            var client = _httpClientFactory.CreateClient("API");
            OrderViewModel? order = null;

            try
            {
                var response = await client.GetAsync($"/api/v1/orders/{id}");
                if (response.IsSuccessStatusCode)
                {
                    order = await response.Content.ReadJsonAsync<OrderViewModel>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Order Details Error: " + ex.Message);
            }

            if (order == null)
            {
                order = new OrderViewModel { Id = id, Status = "Pending", TotalAmount = 0, OrderDate = DateTime.Now };
            }

            return View(order);
        }

    }
}
