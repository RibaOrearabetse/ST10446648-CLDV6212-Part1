using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        product.ImageUrl = imageUrl;
                    }

                    await _storageService.AddEntityAsync(product);

                    // Notify inventory queue about new product
                    var inventoryMsg = JsonSerializer.Serialize(new
                    {
                        type = "product-created",
                        productId = product.ProductId,
                        productName = product.Name,
                        stock = product.StockAvailable,
                        price = product.Price
                    });
                    await _storageService.SendMessageAsync("stocksinventory", inventoryMsg);
                    TempData["Success"] = $"Product '{product.Name}' created successfully with price {product.Price}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                        return NotFound();

                    originalProduct.Name = product.Name;
                    originalProduct.Description = product.Description;
                    originalProduct.Price = product.Price;
                    originalProduct.StockAvailable = product.StockAvailable;

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        originalProduct.ImageUrl = imageUrl;
                    }

                    await _storageService.UpdateEntityAsync(originalProduct);
                    var stockMsg = JsonSerializer.Serialize(new
                    {
                        type = "product-updated",
                        productId = originalProduct.ProductId,
                        productName = originalProduct.Name,
                        stock = originalProduct.StockAvailable
                    });
                    await _storageService.SendMessageAsync("stock-updates", stockMsg);
                    TempData["Success"] = $"Product '{originalProduct.Name}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product");
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        public async Task<IActionResult> Delete(string id, bool? confirm)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Product>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
