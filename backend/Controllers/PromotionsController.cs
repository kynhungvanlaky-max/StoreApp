using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using System.Text.Json;

namespace Backend.Controllers
{
    public class PromotionsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PromotionsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            ViewData["Token"] = Request.Cookies["access_token"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Save(PromotionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("Dữ liệu không hợp lệ.");
                return RedirectToAction("Index");
            }

            var client = _httpClientFactory.CreateClient("API");
            var payload = new
            {
                name = model.Name,
                description = model.Description,
                discountPercent = model.DiscountPercent,
                startDate = model.StartDate,
                endDate = model.EndDate
            };

            HttpResponseMessage response;
            if (model.Id > 0)
            {
                response = await client.PutAsJsonAsync($"/api/promotions/{model.Id}", payload);
                if (response.IsSuccessStatusCode)
                {
                    SetSuccessMessage("Cập nhật khuyến mãi thành công!");
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    SetErrorMessage("Lỗi: " + err);
                }
            }
            else
            {
                response = await client.PostAsJsonAsync("/api/promotions", payload);
                if (response.IsSuccessStatusCode)
                {
                    SetSuccessMessage("Tạo khuyến mãi thành công!");
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    SetErrorMessage("Lỗi: " + err);
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.DeleteAsync($"/api/promotions/{id}");
            SetSuccessMessage(response.IsSuccessStatusCode ? "Đã xóa khuyến mãi thành công!" : "Có lỗi xảy ra khi xóa khuyến mãi.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(int id, int productId)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.PostAsync($"/api/promotions/{id}/products/{productId}", null);
            SetSuccessMessage(response.IsSuccessStatusCode ? "Đã thêm sản phẩm vào khuyến mãi!" : "Có lỗi xảy ra khi thêm sản phẩm.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveProduct(int id, int productId)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.DeleteAsync($"/api/promotions/{id}/products/{productId}");
            SetSuccessMessage(response.IsSuccessStatusCode ? "Đã xóa sản phẩm khỏi khuyến mãi!" : "Có lỗi xảy ra khi xóa sản phẩm.");
            return RedirectToAction("Index");
        }
    }
}
