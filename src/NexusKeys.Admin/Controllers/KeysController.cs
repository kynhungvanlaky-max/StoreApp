using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;

namespace Backend.Controllers
{
    public class KeysController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public KeysController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var client = _httpClientFactory.CreateClient("API");
            var keys = new List<ProductKeyViewModel>();
            var products = new List<ProductDto>();
            int totalPages = 1;
            int totalItems = 0;

            try
            {
                var url = $"/api/v1/product-keys?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(search))
                {
                    url += $"&search={Uri.EscapeDataString(search.Trim())}";
                }
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var paged = await response.Content.ReadJsonAsync<PagedResponse<ProductKeyViewModel>>();
                    if (paged?.Data != null)
                    {
                        keys = paged.Data;
                        totalPages = paged.TotalPages > 0 ? paged.TotalPages : 1;
                        totalItems = paged.TotalItems;
                        page = paged.CurrentPage > 0 ? paged.CurrentPage : 1;
                        pageSize = paged.PageSize > 0 ? paged.PageSize : 10;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Keys Error: " + ex.Message);
            }

            try
            {
                var prodResponse = await client.GetAsync("/api/v1/products?pageSize=1000");
                if (prodResponse.IsSuccessStatusCode)
                {
                    var pagedProds = await prodResponse.Content.ReadJsonAsync<PagedResponse<ProductDto>>();
                    if (pagedProds?.Data != null) products = pagedProds.Data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Prods Error: " + ex.Message);
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;
            ViewBag.Products = products;

            return View(keys);
        }
    }
}

