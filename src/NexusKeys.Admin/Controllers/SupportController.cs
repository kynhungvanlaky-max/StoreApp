using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;

namespace Backend.Controllers
{
    public class SupportController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SupportController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var client = _httpClientFactory.CreateClient("API");
            var tickets = new List<SupportTicketViewModel>();
            int totalPages = 1;
            int totalItems = 0;

            try
            {
                var response = await client.GetAsync($"/api/v1/support-tickets?page={page}&pageSize={pageSize}");
                if (response.IsSuccessStatusCode)
                {
                    var paged = await response.Content.ReadJsonAsync<PagedResponse<SupportTicketViewModel>>();
                    if (paged != null)
                    {
                        tickets = paged.Data;
                        totalPages = paged.TotalPages > 0 ? paged.TotalPages : 1;
                        totalItems = paged.TotalItems;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Support Error: " + ex.Message);
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(tickets);
        }
    }
}
