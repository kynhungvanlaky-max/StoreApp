using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using System.Text.Json;

namespace Backend.Controllers
{
    public class CouponsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CouponsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            ViewData["Token"] = Request.Cookies["access_token"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CouponViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("Dữ liệu không hợp lệ.");
                return RedirectToAction("Index");
            }

            var client = _httpClientFactory.CreateClient("API");
            var payload = new
            {
                code = model.Code,
                discountPercent = model.DiscountPercent,
                maxUsage = model.MaxUsage,
                expiryDate = model.ExpiryDate,
                isActive = model.IsActive
            };
            var response = await client.PostAsJsonAsync("/api/coupons", payload);
            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Tạo mã giảm giá thành công!");
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                SetErrorMessage("Lỗi: " + err);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, CouponViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("Dữ liệu không hợp lệ.");
                return RedirectToAction("Index");
            }

            var client = _httpClientFactory.CreateClient("API");
            var payload = new
            {
                code = model.Code,
                discountPercent = model.DiscountPercent,
                maxUsage = model.MaxUsage,
                expiryDate = model.ExpiryDate,
                isActive = model.IsActive
            };
            var response = await client.PutAsJsonAsync($"/api/coupons/{id}", payload);
            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Cập nhật mã giảm giá thành công!");
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                SetErrorMessage("Lỗi: " + err);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.DeleteAsync($"/api/coupons/{id}");
            SetSuccessMessage(response.IsSuccessStatusCode ? "Đã xóa mã giảm giá thành công!" : "Có lỗi xảy ra khi xóa mã giảm giá.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Save(CouponViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("Dữ liệu không hợp lệ.");
                return RedirectToAction("Index");
            }

            var client = _httpClientFactory.CreateClient("API");
            if (model.Id > 0)
            {
                var payload = new
                {
                    code = model.Code,
                    discountPercent = model.DiscountPercent,
                    maxUsage = model.MaxUsage,
                    expiryDate = model.ExpiryDate,
                    isActive = model.IsActive
                };
                var response = await client.PutAsJsonAsync($"/api/coupons/{model.Id}", payload);
                if (response.IsSuccessStatusCode)
                {
                    SetSuccessMessage("Cập nhật mã giảm giá thành công!");
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    SetErrorMessage("Lỗi: " + err);
                }
            }
            else
            {
                var payload = new
                {
                    code = model.Code,
                    discountPercent = model.DiscountPercent,
                    maxUsage = model.MaxUsage,
                    expiryDate = model.ExpiryDate,
                    isActive = model.IsActive
                };
                var response = await client.PostAsJsonAsync("/api/coupons", payload);
                if (response.IsSuccessStatusCode)
                {
                    SetSuccessMessage("Tạo mã giảm giá thành công!");
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    SetErrorMessage("Lỗi: " + err);
                }
            }

            return RedirectToAction("Index");
        }
    }
}
