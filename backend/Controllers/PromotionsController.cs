using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;
using Backend.Attributes;

namespace Backend.Controllers
{
    [RequireAdminRole]
    public class PromotionsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PromotionsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var promotions = new List<PromotionViewModel>();

            try
            {
                var response = await client.GetAsync("/api/v1/promotions");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadJsonAsync<List<PromotionViewModel>>();
                    if (data != null) promotions = data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Promotions Error: " + ex.Message);
            }

            return View(promotions);
        }
    }
}
