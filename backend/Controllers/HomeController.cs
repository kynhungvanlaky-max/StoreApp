using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;

namespace Backend.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var viewModel = new Backend.Models.DashboardViewModel
        {
            TotalRevenue = 1245000,
            NewUsers = 241,
            TotalOrders = 24,
            ActiveKeys = 1402,
            PendingComments = 5
        };
        return View(viewModel);
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
