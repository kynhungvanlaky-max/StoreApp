using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using System.Text.Json;

namespace Backend.Controllers
{
    public class AuditLogsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuditLogsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            ViewData["Token"] = Request.Cookies["access_token"];

            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/admin/audit?page={page}&pageSize={pageSize}");

            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var pagedResult = JsonSerializer.Deserialize<PagedResponse<AuditLogViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.CurrentPage = pagedResult?.CurrentPage ?? page;
                    ViewBag.TotalPages = pagedResult?.TotalPages ?? 1;
                    ViewBag.TotalItems = pagedResult?.TotalItems ?? 0;
                    ViewBag.PageSize = pagedResult?.PageSize ?? pageSize;
                    return View(new AuditLogListViewModel { Logs = pagedResult?.Data ?? new List<AuditLogViewModel>() });
                }
                catch (Exception ex)
                {
                    SetErrorMessage("Lỗi khi tải dữ liệu audit: " + ex.Message);
                    return View(new AuditLogListViewModel());
                }
            }

            SetErrorMessage("Không thể lấy danh sách audit log.");
            return View(new AuditLogListViewModel());
        }
    }
}
