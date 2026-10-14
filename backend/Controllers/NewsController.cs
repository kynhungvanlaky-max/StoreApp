using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;

namespace Backend.Controllers
{
    public class NewsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public NewsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var client = _httpClientFactory.CreateClient("API");
            var news = new List<NewsViewModel>();
            int totalPages = 1;
            int totalItems = 0;

            try
            {
                var response = await client.GetAsync($"/api/v1/news?page={page}&pageSize={pageSize}");
                if (response.IsSuccessStatusCode)
                {
                    var paged = await response.Content.ReadJsonAsync<PagedResponse<NewsViewModel>>();
                    if (paged != null)
                    {
                        news = paged.Data;
                        totalPages = paged.TotalPages > 0 ? paged.TotalPages : 1;
                        totalItems = paged.TotalItems;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API News Error: " + ex.Message);
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(news);
        }
    }
}
