using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;
using Backend.Attributes;

namespace Backend.Controllers
{
    [RequireAdminRole]
    public class PaymentConfigsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentConfigsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var configs = new List<PaymentConfigViewModel>();

            try
            {
                var response = await client.GetAsync("/api/v1/payment-configurations");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadJsonAsync<List<PaymentConfigViewModel>>();
                    if (data != null) configs = data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API PaymentConfigs Error: " + ex.Message);
            }

            return View(configs);
        }
    }
}
