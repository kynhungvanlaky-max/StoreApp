using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using System.Text.Json;

namespace Backend.Controllers
{
    public class TransactionsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TransactionsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            ViewData["Token"] = Request.Cookies["access_token"];
            
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/wallet/admin/all?page={page}&pageSize={pageSize}");
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var pagedResult = JsonSerializer.Deserialize<PagedResponse<TransactionViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.CurrentPage = pagedResult?.CurrentPage ?? page;
                    ViewBag.TotalPages = pagedResult?.TotalPages ?? 1;
                    ViewBag.TotalItems = pagedResult?.TotalItems ?? 0;
                    ViewBag.PageSize = pagedResult?.PageSize ?? pageSize;
                    return View(new TransactionListViewModel { Transactions = pagedResult?.Data ?? new List<TransactionViewModel>() });
                }
                catch (Exception ex)
                {
                    SetErrorMessage("Lỗi khi tải dữ liệu giao dịch: " + ex.Message);
                    return View(new TransactionListViewModel());
                }
            }

            SetErrorMessage("Không thể lấy danh sách giao dịch.");
            return View(new TransactionListViewModel());
        }
    }
}
