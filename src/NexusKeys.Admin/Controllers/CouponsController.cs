using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;
using Backend.Attributes;

namespace Backend.Controllers
{
    [RequireAdminRole]
    public class CouponsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CouponsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var coupons = new List<CouponViewModel>();

            try
            {
                var response = await client.GetAsync("/api/v1/coupons");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadJsonAsync<List<CouponViewModel>>();
                    if (data != null) coupons = data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Coupons Error: " + ex.Message);
            }

            return View(coupons);
        }
    }
}
