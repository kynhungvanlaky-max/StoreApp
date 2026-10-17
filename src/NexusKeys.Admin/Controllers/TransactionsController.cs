using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;
using Backend.Attributes;

namespace Backend.Controllers
{
    [RequireAdminRole]
    public class TransactionsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TransactionsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var transactions = new List<TransactionViewModel>();

            try
            {
                var response = await client.GetAsync("/api/v1/transactions?page=1&pageSize=100");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadJsonAsync<PagedResponse<TransactionViewModel>>();
                    if (data?.Data != null) transactions = data.Data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Transactions Error: " + ex.Message);
            }

            return View(transactions);
        }
    }
}
