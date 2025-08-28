using System.Diagnostics;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAzureStorageService _storage;

        public HomeController(ILogger<HomeController> logger, IAzureStorageService storage)
        {
            _logger = logger;
            _storage = storage;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _storage.GetAllEntitiesAsync<CustomerDetails>();
            var products = await _storage.GetAllEntitiesAsync<Product>();
            var orders = await _storage.GetAllEntitiesAsync<Order>();

            ViewBag.CustomerCount = customers.Count;
            ViewBag.ProductCount = products.Count;
            ViewBag.OrderCount = orders.Count;
            ViewBag.FeaturedProducts = products.Take(5).ToList();
            return View();
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
}
