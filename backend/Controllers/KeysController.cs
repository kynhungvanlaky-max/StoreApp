using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    using Backend.Models;
    using System.Text.Json;
    using System.Text;

    public class KeysController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public KeysController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/keys?page={page}&pageSize={pageSize}");

            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            var viewModel = new KeyListViewModel();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<ProductKeyViewModel>>();
                    ViewBag.CurrentPage = pagedResult?.CurrentPage ?? page;
                    ViewBag.TotalPages = pagedResult?.TotalPages ?? 1;
                    ViewBag.TotalItems = pagedResult?.TotalItems ?? 0;
                    ViewBag.PageSize = pagedResult?.PageSize ?? pageSize;
                    if (pagedResult?.Data != null)
                    {
                        foreach (var k in pagedResult.Data)
                        {
                            k.Status = k.IsSold ? "Đã bán" : "Chưa bán";
                        }
                        viewModel.Keys = pagedResult.Data;
                    }
                }
                catch (Exception ex)
                {
                    SetErrorMessage("Lỗi khi tải dữ liệu mã game: " + ex.Message);
                }
            }

            var prodResponse = await client.GetAsync("/api/products?pageSize=1000");
            if (prodResponse.IsSuccessStatusCode)
            {
                var pagedProds = await prodResponse.Content.ReadFromJsonAsync<PagedResponse<ProductDto>>();
                viewModel.Products = pagedProds?.Data ?? new List<ProductDto>();
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Generate(int productId, int quantity)
        {
            var client = _httpClientFactory.CreateClient("API");
            int successCount = 0;
            for (int i = 0; i < quantity; i++)
            {
                string randomKey = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
                string formattedKey = $"{randomKey.Substring(0,4)}-{randomKey.Substring(4,4)}-{randomKey.Substring(8,4)}-{randomKey.Substring(12,4)}";

                var jsonContent = new StringContent(JsonSerializer.Serialize(new { ProductId = productId, KeyCode = formattedKey, IsSold = false }), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/api/keys", jsonContent);
                
                var redirectResult = HandleApiResponse(response);
                if (redirectResult != null) return redirectResult;

                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                }
            }

            SetSuccessMessage($"Đã tạo thành công {successCount}/{quantity} key mới.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.DeleteAsync($"/api/keys/{id}");

            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Đã xóa mã game thành công!");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi xóa mã game.");
            }

            return RedirectToAction("Index");
        }
    }
}
