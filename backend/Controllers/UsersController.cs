using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;
using Backend.Attributes;

namespace Backend.Controllers
{
    [RequireAdminRole]
    public class UsersController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UsersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var client = _httpClientFactory.CreateClient("API");
            var users = new List<UserViewModel>();
            int totalPages = 1;
            int totalItems = 0;

            try
            {
                var url = $"/api/v1/users?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var paged = await response.Content.ReadJsonAsync<PagedResponse<UserViewModel>>();
                    if (paged != null)
                    {
                        users = paged.Data;
                        totalPages = paged.TotalPages > 0 ? paged.TotalPages : 1;
                        totalItems = paged.TotalItems;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Users Error: " + ex.Message);
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.Search = search;

            return View(users);
        }
    }
}
