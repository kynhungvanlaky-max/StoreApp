using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    using Backend.Models;
    using System.Text.Json;
    using System.Text;

    public class NewsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public NewsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public class NewsResponseDto
        {
            public List<NewsViewModel> Data { get; set; } = new List<NewsViewModel>();
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/news?page={page}&pageSize={pageSize}");
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<NewsViewModel>>();
                    ViewBag.CurrentPage = pagedResult?.CurrentPage ?? page;
                    ViewBag.TotalPages = pagedResult?.TotalPages ?? 1;
                    ViewBag.TotalItems = pagedResult?.TotalItems ?? 0;
                    ViewBag.PageSize = pagedResult?.PageSize ?? pageSize;
                    return View(new NewsListViewModel { NewsArticles = pagedResult?.Data ?? new List<NewsViewModel>() });
                }
                catch (Exception ex)
                {
                    SetErrorMessage("Lỗi khi tải dữ liệu tin tức: " + ex.Message);
                    return View(new NewsListViewModel());
                }
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
            var jsonContent = new StringContent(JsonSerializer.Serialize(new { Title = model.Title, Summary = model.Summary, Content = model.Content, CoverImageUrl = model.CoverImageUrl }), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/news", jsonContent);

            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Đăng tin tức mới thành công!");
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Lỗi khi lưu tin tức vào API.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.DeleteAsync($"/api/news/{id}");

            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Đã xóa tin tức thành công!");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi xóa tin tức.");
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/news/{id}");

            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                var article = await response.Content.ReadFromJsonAsync<NewsViewModel>();
                return View(article);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, NewsViewModel model)
        {
            var client = _httpClientFactory.CreateClient("API");
            var jsonContent = new StringContent(JsonSerializer.Serialize(new { Title = model.Title, Summary = model.Summary, Content = model.Content, CoverImageUrl = model.CoverImageUrl }), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/news/{id}", jsonContent);

            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Cập nhật tin tức thành công!");
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Lỗi khi cập nhật tin tức vào API.");
            return View(model);
        }
    }
}
