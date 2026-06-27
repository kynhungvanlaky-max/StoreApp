using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;

namespace Backend.Controllers;

public class HomeController : BaseController
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var client = _httpClientFactory.CreateClient("API");
        var response = await client.GetAsync("/api/admin/dashboard");
        
        var apiResponse = HandleApiResponse(response);
        if (apiResponse != null) return apiResponse;

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<DashboardViewModel>();
            if (data != null)
            {
                return View(data);
            }
        }

        return View(new DashboardViewModel());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
