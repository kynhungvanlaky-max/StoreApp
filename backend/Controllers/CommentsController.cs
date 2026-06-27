using Microsoft.AspNetCore.Mvc;


namespace Backend.Controllers
{
    using Backend.Models;

    public class CommentsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CommentsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.GetAsync($"/api/reviews/admin/all?page={page}&pageSize={pageSize}");
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var pagedResult = await response.Content.ReadFromJsonAsync<PagedResponse<CommentViewModel>>();
                    ViewBag.CurrentPage = pagedResult?.CurrentPage ?? page;
                    ViewBag.TotalPages = pagedResult?.TotalPages ?? 1;
                    ViewBag.TotalItems = pagedResult?.TotalItems ?? 0;
                    ViewBag.PageSize = pagedResult?.PageSize ?? pageSize;
                    return View(new CommentListViewModel { Comments = pagedResult?.Data ?? new List<CommentViewModel>() });
                }
                catch (Exception ex)
                {
                    SetErrorMessage("Lỗi khi tải dữ liệu đánh giá: " + ex.Message);
                    return View(new CommentListViewModel());
                }
            }

            return View(new CommentListViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.PutAsync($"/api/reviews/{id}/hide", null);
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Thay đổi trạng thái hiển thị thành công!");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi thay đổi trạng thái.");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.DeleteAsync($"/api/reviews/{id}");
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Đã xóa đánh giá thành công!");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi xóa đánh giá.");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleReplyVisibility(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.PutAsync($"/api/reviews/comments/{id}/hide", null);
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Thay đổi trạng thái bình luận phản hồi thành công!");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi thay đổi trạng thái phản hồi.");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var client = _httpClientFactory.CreateClient("API");
            var response = await client.DeleteAsync($"/api/reviews/comments/{id}");
            
            var redirectResult = HandleApiResponse(response);
            if (redirectResult != null) return redirectResult;

            if (response.IsSuccessStatusCode)
            {
                SetSuccessMessage("Đã xóa phản hồi thành công!");
            }
            else
            {
                SetErrorMessage("Có lỗi xảy ra khi xóa phản hồi.");
            }

            return RedirectToAction("Index");
        }
    }
}
