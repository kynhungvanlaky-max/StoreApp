using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    public class UsersController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UsersController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            ViewData["Token"] = Request.Cookies["access_token"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.PutAsync($"/api/users/{id}/toggle-lock", null);
            
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
        public async Task<IActionResult> ChangeRole(int id, string roleName)
        {
            var client = _httpClientFactory.CreateClient("API");
            var dto = new { RoleName = roleName };
            var response = await client.PutAsJsonAsync($"/api/users/{id}/role", dto);
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Cập nhật quyền thành công.");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi cập nhật quyền.");
            }

            return RedirectToAction("Index");
        }
    }
}
