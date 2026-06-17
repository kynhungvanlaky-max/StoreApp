using Microsoft.AspNetCore.Mvc;

namespace Backend.Models
{
    public class ReviewReplyViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }
    }

    public class CommentViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }
        public List<ReviewReplyViewModel> Replies { get; set; } = new();
    }
}

namespace Backend.Controllers
{
    using Backend.Models;

    public class CommentsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CommentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.GetAsync("/api/reviews/admin/all");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                var reviews = await response.Content.ReadFromJsonAsync<List<CommentViewModel>>();
                return View(reviews ?? new List<CommentViewModel>());
            }

            return View(new List<CommentViewModel>());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PutAsync($"/api/reviews/{id}/hide", null);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Thay đổi trạng thái hiển thị thành công!";
            }
            else
            {
                TempData["Error"] = "Có lỗi xảy ra khi thay đổi trạng thái.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.DeleteAsync($"/api/reviews/{id}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Đã xóa đánh giá thành công!";
            }
            else
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa đánh giá.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleReplyVisibility(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PutAsync($"/api/reviews/comments/{id}/hide", null);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Thay đổi trạng thái bình luận phản hồi thành công!";
            }
            else
            {
                TempData["Error"] = "Có lỗi xảy ra khi thay đổi trạng thái phản hồi.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.DeleteAsync($"/api/reviews/comments/{id}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Đã xóa phản hồi thành công!";
            }
            else
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa phản hồi.";
            }

            return RedirectToAction("Index");
        }
    }
}
