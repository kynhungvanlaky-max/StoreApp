using Microsoft.AspNetCore.Mvc;

namespace Backend.Models
{
    public class NewsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}

namespace Backend.Controllers
{
    using Backend.Models;
    using System.Text.Json;
    using System.Text;

    public class NewsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public NewsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync("/api/news");

            if (response.IsSuccessStatusCode)
            {
                var news = await response.Content.ReadFromJsonAsync<List<NewsViewModel>>();
                return View(new NewsListViewModel { NewsArticles = news ?? new List<NewsViewModel>() });
            }

            return View(new NewsListViewModel());
        }

        public IActionResult Create()
        {
            return View(new NewsViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(NewsViewModel model)
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var jsonContent = new StringContent(JsonSerializer.Serialize(new { Title = model.Title, Summary = model.Summary, Content = model.Content, CoverImageUrl = model.CoverImageUrl }), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/news", jsonContent);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đăng tin tức mới thành công!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Lỗi khi lưu tin tức vào API.");
            return View(model);
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

            var response = await client.DeleteAsync($"/api/news/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Redirect("http://localhost:3000/Auth/Login");
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đã xóa tin tức thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa tin tức.";
            }

            return RedirectToAction("Index");
        }
    }
}
