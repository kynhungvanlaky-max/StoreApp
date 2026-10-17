using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api-proxy/{**path}")]
    [AutoValidateAntiforgeryToken]
    public class ApiProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Forward(string? path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !path.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Đường dẫn API không hợp lệ." });
            }

            var client = _httpClientFactory.CreateClient("API");
            var target = path + Request.QueryString;
            using var request = new HttpRequestMessage(new HttpMethod(Request.Method), target);

            if (Request.ContentLength > 0 || Request.Headers.ContainsKey(HeaderNames.TransferEncoding))
            {
                request.Content = new StreamContent(Request.Body);
                if (!string.IsNullOrWhiteSpace(Request.ContentType))
                {
                    request.Content.Headers.TryAddWithoutValidation(HeaderNames.ContentType, Request.ContentType);
                }
            }

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
            Response.StatusCode = (int)response.StatusCode;
            return File(content, contentType, enableRangeProcessing: false);
        }
    }
}
