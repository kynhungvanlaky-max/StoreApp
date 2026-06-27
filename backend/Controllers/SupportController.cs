using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using System.Text.Json;

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
            ViewData["Token"] = Request.Cookies["access_token"];
            
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/support/admin/all?page={page}&pageSize={pageSize}");
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var pagedResult = JsonSerializer.Deserialize<PagedResponse<SupportTicketViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.CurrentPage = pagedResult?.CurrentPage ?? page;
                    ViewBag.TotalPages = pagedResult?.TotalPages ?? 1;
                    ViewBag.TotalItems = pagedResult?.TotalItems ?? 0;
                    ViewBag.PageSize = pagedResult?.PageSize ?? pageSize;
                    return View(new SupportListViewModel { Tickets = pagedResult?.Data ?? new List<SupportTicketViewModel>() });
                }
                catch (Exception ex)
                {
                    SetErrorMessage("Lỗi khi tải dữ liệu hỗ trợ: " + ex.Message);
                    return View(new SupportListViewModel());
                }
            }

            SetErrorMessage("Không thể lấy danh sách yêu cầu hỗ trợ.");
            return View(new SupportListViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var client = _httpClientFactory.CreateClient("API");
            var dto = new { Status = status };
            var response = await client.PutAsJsonAsync($"/api/support/admin/{id}/status", dto);
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Cập nhật trạng thái thành công.");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi cập nhật trạng thái.");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reply(int id, string replyContent)
        {
            var client = _httpClientFactory.CreateClient("API");
            var dto = new { ReplyContent = replyContent };
            var response = await client.PutAsJsonAsync($"/api/support/admin/{id}/reply", dto);

            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Gửi phản hồi thành công!");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi gửi phản hồi.");
            }

            return RedirectToAction("Index");
        }
    }
}
