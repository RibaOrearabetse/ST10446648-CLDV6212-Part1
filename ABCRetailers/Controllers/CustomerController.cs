using ABCRetailers.Models;
using ABCRetailers.Services;
using Azure; 
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IAzureStorageService _storage;
        private const string Partition = "Customer";

        public CustomerController(IAzureStorageService storage)
        {
            _storage = storage;
        }

        // GET: /Customer
        public async Task<IActionResult> Index()
        {
            var customers = await _storage.GetAllEntitiesAsync<CustomerDetails>();
            // optional: sort by Surname then Name
            customers = customers
                .OrderBy(c => c.Surname)
                .ThenBy(c => c.Name)
                .ToList();
            return View(customers);
        }

        // GET: /Customer/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _storage.GetEntityAsync<CustomerDetails>(Partition, id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        // GET: /Customer/Create
        public IActionResult Create()
        {
            return View(new CustomerDetails());
        }

        // POST: /Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerDetails model)
        {
            // Ensure partition + id are present
            model.PartitionKey = Partition;
            if (string.IsNullOrWhiteSpace(model.RowKey))
                model.RowKey = Guid.NewGuid().ToString();

            // Basic server-side validation
            if (!ModelState.IsValid)
                return View(model);

            await _storage.AddEntityAsync(model);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Customer/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _storage.GetEntityAsync<CustomerDetails>(Partition, id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        // POST: /Customer/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CustomerDetails model)
        {
            if (id != model.RowKey) return BadRequest();

            // Keep partition key constant
            model.PartitionKey = Partition;

            // Ignore optimistic concurrency for now
            model.ETag = ETag.All;

            if (!ModelState.IsValid)
                return View(model);

            await _storage.UpdateEntityAsync(model);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Customer/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var entity = await _storage.GetEntityAsync<CustomerDetails>(Partition, id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        // POST: /Customer/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            await _storage.DeleteEntityAsync<CustomerDetails>(Partition, id);
            return RedirectToAction(nameof(Index));
        }
    }
}
