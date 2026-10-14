using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;
using Backend.Attributes;

namespace Backend.Controllers
{
    [RequireAdminRole]
    public class AuditLogsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuditLogsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var client = _httpClientFactory.CreateClient("API");
            var logs = new List<AuditLogViewModel>();
            int totalPages = 1;
            int totalItems = 0;

            try
            {
                var response = await client.GetAsync($"/api/v1/audit-logs?page={page}&pageSize={pageSize}");
                if (response.IsSuccessStatusCode)
                {
                    var paged = await response.Content.ReadJsonAsync<PagedResponse<AuditLogViewModel>>();
                    if (paged != null)
                    {
                        logs = paged.Data;
                        totalPages = paged.TotalPages > 0 ? paged.TotalPages : 1;
                        totalItems = paged.TotalItems;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API AuditLogs Error: " + ex.Message);
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(logs);
        }
    }
}
