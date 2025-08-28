// Controllers/OrderController.cs
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ABCRetailers.Models.ViewModels;
using Azure;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<CustomerDetails>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

            var vm = new OrderCreateViewModel
            {
                Customers = customers.Select(c => new SelectListItem
                {
                    Value = c.RowKey,
                    Text = string.IsNullOrWhiteSpace(c.Username) ? $"{c.Name} {c.Surname}" : c.Username
                }).ToList(),
                Products = products.Select(p => new SelectListItem
                {
                    Value = p.RowKey,
                    Text = p.Name
                }).ToList(),
                OrderDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc)
            };

            return View(vm);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // repopulate dropdowns
                var customersList = await _storageService.GetAllEntitiesAsync<CustomerDetails>();
                var productsList = await _storageService.GetAllEntitiesAsync<Product>();
                model.Customers = customersList.Select(c => new SelectListItem { Value = c.RowKey, Text = string.IsNullOrWhiteSpace(c.Username) ? $"{c.Name} {c.Surname}" : c.Username }).ToList();
                model.Products = productsList.Select(p => new SelectListItem { Value = p.RowKey, Text = p.Name }).ToList();
                return View(model);
            }

            // Fetch related entities
            var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);
            var customer = await _storageService.GetEntityAsync<CustomerDetails>("Customer", model.CustomerId);

            if (product == null)
            {
                ModelState.AddModelError(nameof(model.ProductId), "Selected product does not exist.");
            }
            if (customer == null)
            {
                ModelState.AddModelError(nameof(model.CustomerId), "Selected customer does not exist.");
            }
            if (!ModelState.IsValid)
            {
                var customersList2 = await _storageService.GetAllEntitiesAsync<CustomerDetails>();
                var productsList2 = await _storageService.GetAllEntitiesAsync<Product>();
                model.Customers = customersList2.Select(c => new SelectListItem { Value = c.RowKey, Text = string.IsNullOrWhiteSpace(c.Username) ? $"{c.Name} {c.Surname}" : c.Username }).ToList();
                model.Products = productsList2.Select(p => new SelectListItem { Value = p.RowKey, Text = p.Name }).ToList();
                return View(model);
            }

            var order = new Order
            {
                PartitionKey = "Order",
                RowKey = Guid.NewGuid().ToString(),
                CustomerId = model.CustomerId,
                Username = customer!.Username,
                ProductId = model.ProductId,
                ProductName = product!.Name,
                OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc),
                Quantity = model.Quantity,
                Price = Convert.ToDouble(product.Price),
                Status = model.Status
            };
            order.TotalPrice = order.Quantity * order.Price;

            await _storageService.AddEntityAsync(order);

            // Apply inventory for initial status: deduct unless Cancelled
            if (!string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                var prodForCreate = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                if (prodForCreate != null)
                {
                    var prev = prodForCreate.StockAvailable;
                    prodForCreate.StockAvailable = Math.Max(0, prodForCreate.StockAvailable - order.Quantity);
                    await _storageService.UpdateEntityAsync(prodForCreate);

                    var createStockMsg = JsonSerializer.Serialize(new
                    {
                        type = "stock-update",
                        productId = prodForCreate.ProductId,
                        productName = prodForCreate.Name,
                        change = -order.Quantity,
                        previous = prev,
                        current = prodForCreate.StockAvailable,
                        reason = $"order-created-{order.Status.ToLower()}",
                        orderId = order.OrderId
                    });
                    await _storageService.SendMessageAsync("stock-updates", createStockMsg);
                }
            }

            var orderMsg = JsonSerializer.Serialize(new
            {
                type = "order-created",
                orderId = order.OrderId,
                customer = order.Username,
                productId = order.ProductId,
                productName = order.ProductName,
                quantity = order.Quantity,
                total = order.TotalPrice
            });
            await _storageService.SendMessageAsync("order-notifications", orderMsg);
                return RedirectToAction(nameof(Index));
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
                return NotFound();
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            ViewBag.Products = new SelectList(products, nameof(Product.RowKey), nameof(Product.Name), order.ProductId);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                // Load original order for inventory reconciliation
                var existing = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);
                if (existing == null)
                    return NotFound();

                order.PartitionKey = "Order";
                // update product-derived fields based on potentially new product selection
                var newProduct = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                if (newProduct != null)
                {
                    order.ProductName = newProduct.Name;
                    order.Price = Convert.ToDouble(newProduct.Price);
                }

                // normalize date to UTC and recompute total
                order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                order.TotalPrice = order.Quantity * order.Price;
                // ignore concurrency on this simple edit path
                order.ETag = ETag.All;
                await _storageService.UpdateEntityAsync(order);

                // Determine how to apply inventory based on status transitions
                bool wasCancelled = string.Equals(existing.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
                bool nowCancelled = string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);

                if (wasCancelled && !nowCancelled)
                {
                    // Previously not deducted; deduct now on reactivation
                    if (newProduct != null)
                    {
                        var prev = newProduct.StockAvailable;
                        newProduct.StockAvailable = Math.Max(0, newProduct.StockAvailable - order.Quantity);
                        await _storageService.UpdateEntityAsync(newProduct);
                        var msg = JsonSerializer.Serialize(new
                        {
                            type = "stock-update",
                            productId = newProduct.ProductId,
                            productName = newProduct.Name,
                            change = -order.Quantity,
                            previous = prev,
                            current = newProduct.StockAvailable,
                            reason = "order-status-reactivated",
                            orderId = order.OrderId
                        });
                        await _storageService.SendMessageAsync("stock-updates", msg);
                    }
                }
                else if (!wasCancelled && nowCancelled)
                {
                    // Was deducted; restore stock when cancelling
                    var oldProduct = await _storageService.GetEntityAsync<Product>("Product", existing.ProductId);
                    if (oldProduct != null)
                    {
                        var prev = oldProduct.StockAvailable;
                        oldProduct.StockAvailable = prev + existing.Quantity;
                        await _storageService.UpdateEntityAsync(oldProduct);
                        var msg = JsonSerializer.Serialize(new
                        {
                            type = "stock-update",
                            productId = oldProduct.ProductId,
                            productName = oldProduct.Name,
                            change = existing.Quantity,
                            previous = prev,
                            current = oldProduct.StockAvailable,
                            reason = "order-cancelled",
                            orderId = order.OrderId
                        });
                        await _storageService.SendMessageAsync("stock-updates", msg);
                    }
                }
                else if (!nowCancelled && existing.ProductId != order.ProductId)
                {
                    // Return stock to old product
                    var oldProduct = await _storageService.GetEntityAsync<Product>("Product", existing.ProductId);
                    if (oldProduct != null)
                    {
                        var prev = oldProduct.StockAvailable;
                        oldProduct.StockAvailable = prev + existing.Quantity;
                        await _storageService.UpdateEntityAsync(oldProduct);
                        var restoreMsg = JsonSerializer.Serialize(new
                        {
                            type = "stock-update",
                            productId = oldProduct.ProductId,
                            productName = oldProduct.Name,
                            change = existing.Quantity,
                            previous = prev,
                            current = oldProduct.StockAvailable,
                            reason = "order-edit-product-change-restore",
                            orderId = order.OrderId
                        });
                        await _storageService.SendMessageAsync("stock-updates", restoreMsg);
                    }
                    // Deduct stock from new product
                    if (newProduct != null)
                    {
                        var prev = newProduct.StockAvailable;
                        newProduct.StockAvailable = Math.Max(0, newProduct.StockAvailable - order.Quantity);
                        await _storageService.UpdateEntityAsync(newProduct);
                        var deductMsg = JsonSerializer.Serialize(new
                        {
                            type = "stock-update",
                            productId = newProduct.ProductId,
                            productName = newProduct.Name,
                            change = -order.Quantity,
                            previous = prev,
                            current = newProduct.StockAvailable,
                            reason = "order-edit-product-change-deduct",
                            orderId = order.OrderId
                        });
                        await _storageService.SendMessageAsync("stock-updates", deductMsg);
                    }
                }
                else if (!nowCancelled && existing.Quantity != order.Quantity)
                {
                    // Same product, adjust by delta
                    var delta = order.Quantity - existing.Quantity; // positive means more items ordered
                    var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
                    if (product != null && delta != 0)
                    {
                        var prev = product.StockAvailable;
                        product.StockAvailable = delta > 0
                            ? Math.Max(0, product.StockAvailable - delta)
                            : product.StockAvailable + (-delta);
                        await _storageService.UpdateEntityAsync(product);
                        var adjMsg = JsonSerializer.Serialize(new
                        {
                            type = "stock-update",
                            productId = product.ProductId,
                            productName = product.Name,
                            change = -delta, // negative when increasing qty
                            previous = prev,
                            current = product.StockAvailable,
                            reason = "order-edit-quantity-change",
                            orderId = order.OrderId
                        });
                        await _storageService.SendMessageAsync("stock-updates", adjMsg);
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            ViewBag.Products = new SelectList(products, nameof(Product.RowKey), nameof(Product.Name), order.ProductId);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            // Restore inventory on delete only if stock was deducted (i.e., not Cancelled)
            var existing = await _storageService.GetEntityAsync<Order>("Order", id);
            if (existing != null && !string.Equals(existing.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", existing.ProductId);
                if (product != null)
                {
                    var prev = product.StockAvailable;
                    product.StockAvailable = prev + existing.Quantity;
                    await _storageService.UpdateEntityAsync(product);
                    var msg = JsonSerializer.Serialize(new
                    {
                        type = "stock-update",
                        productId = product.ProductId,
                        productName = product.Name,
                        change = existing.Quantity,
                        previous = prev,
                        current = product.StockAvailable,
                        reason = "order-deleted",
                        orderId = existing.OrderId
                    });
                    await _storageService.SendMessageAsync("stock-updates", msg);
                }
            }

            await _storageService.DeleteEntityAsync<Order>("Order", id);
            return RedirectToAction(nameof(Index));
        }
    }
}
