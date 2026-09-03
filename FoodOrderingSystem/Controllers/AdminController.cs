using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("IsAdmin") == "True";
        }
        public IActionResult Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalItems = _context.FoodItems.Count();
            return View();
        }

        //a primo giro param tutti null (tipi nullable e stringhe possono essere null), ma se clicco su un filtro o su un ordinamento, ASP.NET Core mi passa i valori dei parametri
        public IActionResult ManageMenu(int? categoryId, string searchString, bool? isAvailable, decimal? minPrice, decimal? maxPrice, string sortOrder)   
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // Store sort order for view
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParam = sortOrder == "name_desc" ? "name" : "name_desc";
            ViewBag.PriceSortParam = sortOrder == "price_asc" ? "price_desc" : "price_asc";             //all'inizio vale price asc e poi a ogni giro cambia tra asc e desc
            ViewBag.CategorySortParam = sortOrder == "category" ? "category_desc" : "category";

            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalItems = _context.FoodItems.Count();
            ViewBag.AvailableItems = _context.FoodItems.Where(i => i.IsAvailable).Count();
            ViewBag.UnavailableItems = _context.FoodItems.Where(i => !i.IsAvailable).Count();

            var items = _context.FoodItems
               .Include(f => f.Category)
               .AsQueryable();

            // Filter items by Category
            if (categoryId.HasValue && categoryId > 0)
            {
                items = items.Where(f => f.CategoryId == categoryId);
                ViewBag.CurrentCategory = categoryId;
            }

            // Filter by Search String (Name or Description)
            if (!string.IsNullOrEmpty(searchString))
            {
                items = items.Where(f =>
                    f.Name.Contains(searchString) ||
                    f.Description.Contains(searchString));
                ViewBag.CurrentSearch = searchString;
            }

            // Filter by Availability
            if (isAvailable.HasValue)
            {
                items = items.Where(f => f.IsAvailable == isAvailable.Value);
                ViewBag.CurrentAvailability = isAvailable.Value;
            }

            // Filter by Price Range
            if (minPrice.HasValue)
            {
                items = items.Where(f => f.Price >= minPrice.Value);
                ViewBag.MinPrice = minPrice.Value;
            }
            if (maxPrice.HasValue)
            {
                items = items.Where(f => f.Price <= maxPrice.Value);
                ViewBag.MaxPrice = maxPrice.Value;
            }

            //ordino gli items
            items = sortOrder switch
            {
                "name" => items.OrderBy(f => f.Name),
                "name_desc" => items.OrderByDescending(f => f.Name),

                "price_asc" => items.OrderBy(f => f.Price),
                "price_desc" => items.OrderByDescending(f => f.Price),

                "category" => items.OrderBy(f => f.Category.Name),
                "category_desc" => items.OrderByDescending(f => f.Category.Name),

                _ => items.OrderBy(f => f.Name)         //primo giro scatta solo questo
            };

            ViewBag.Categories = _context.Categories.ToList();

            return View(items.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleAvailability(int id)
        {
            // Security check: Only Admins can toggle availability
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var item = _context.FoodItems.Find(id);
            if (item == null) return NotFound();

            // Logic to flip the availability status
            item.IsAvailable = !item.IsAvailable;
            _context.SaveChanges();

            // Set notification message for the user
            TempData["Success"] = $"{item.Name} is now {(item.IsAvailable ? "available" : "unavailable")}";

            return RedirectToAction("ManageMenu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFoodItem(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var item = await _context.FoodItems.FindAsync(id);
            if (item == null)
            {
                TempData["Error"] = "The selected item no longer exists.";
                return RedirectToAction("ManageMenu");
            }

            var itemName = item.Name;
            var imageUrl = item.ImageUrl;

            _context.FoodItems.Remove(item);
            await _context.SaveChangesAsync();

            // Uploaded images use a GUID filename; keep seeded/static assets intact.
            var imageFileName = Path.GetFileName(imageUrl);
            if (imageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParseExact(Path.GetFileNameWithoutExtension(imageFileName), "N", out _))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", imageFileName);

                if (System.IO.File.Exists(imagePath) &&
                    !await _context.FoodItems.AnyAsync(foodItem => foodItem.ImageUrl == imageUrl))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            TempData["Success"] = $"{itemName} was deleted successfully.";
            return RedirectToAction("ManageMenu");
        }

        [HttpGet]
        public async Task<IActionResult> EditFoodItem(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var item = await _context.FoodItems.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFoodItem(int id, FoodItem item, IFormFile? imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            if (id != item.Id) return NotFound();

            var existingItem = await _context.FoodItems.FindAsync(id);
            if (existingItem == null) return NotFound();

            ModelState.Remove(nameof(FoodItem.ImageUrl));
            ModelState.Remove(nameof(FoodItem.Category));
            ModelState.Remove(nameof(FoodItem.Description));
            item.Description ??= string.Empty;

            const long maxImageSize = 5 * 1024 * 1024;
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp"
            };

            if (imageFile is { Length: > 0 })
            {
                var extension = Path.GetExtension(imageFile.FileName);
                if (imageFile.Length > maxImageSize)
                {
                    ModelState.AddModelError(nameof(imageFile), "The image cannot exceed 5 MB.");
                }
                else if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(imageFile), "Use a JPG, PNG, GIF or WEBP image.");
                }
            }

            if (!ModelState.IsValid)
            {
                item.ImageUrl = existingItem.ImageUrl;
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View(item);
            }

            existingItem.Name = item.Name;
            existingItem.Description = item.Description;
            existingItem.Price = item.Price;
            existingItem.CategoryId = item.CategoryId;
            existingItem.IsAvailable = item.IsAvailable;

            var oldImageUrl = existingItem.ImageUrl;
            if (imageFile is { Length: > 0 })
            {
                var uploadsDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                Directory.CreateDirectory(uploadsDirectory);

                var imageFileName = $"{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName).ToLowerInvariant()}";
                var imagePath = Path.Combine(uploadsDirectory, imageFileName);
                await using (var stream = new FileStream(imagePath, FileMode.CreateNew))
                {
                    await imageFile.CopyToAsync(stream);
                }

                existingItem.ImageUrl = $"/images/{imageFileName}";
            }

            await _context.SaveChangesAsync();

            if (imageFile is { Length: > 0 })
            {
                await DeleteUploadedImageIfUnused(oldImageUrl);
            }

            TempData["Success"] = $"{existingItem.Name} was updated successfully.";
            return RedirectToAction("ManageMenu");
        }

        [HttpGet]
        public IActionResult AddFoodItem()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFoodItem(FoodItem item, IFormFile imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            ModelState.Remove(nameof(FoodItem.ImageUrl));
            ModelState.Remove(nameof(FoodItem.Description));
            item.Description ??= string.Empty;

            const long maxImageSize = 5 * 1024 * 1024;
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp"
            };

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError(nameof(imageFile), "Select an image for the item.");
            }
            else if (imageFile.Length > maxImageSize)
            {
                ModelState.AddModelError(nameof(imageFile), "The image cannot exceed 5 MB.");
            }
            else
            {
                var extension = Path.GetExtension(imageFile.FileName);
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(imageFile), "Use a JPG, PNG, GIF or WEBP image.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.ToList();
                return View(item);
            }

            var uploadsDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            Directory.CreateDirectory(uploadsDirectory);

            var uploadedImage = imageFile!;
            var imageFileName = $"{Guid.NewGuid():N}{Path.GetExtension(uploadedImage.FileName).ToLowerInvariant()}";
            var imagePath = Path.Combine(uploadsDirectory, imageFileName);
            await using (var stream = new FileStream(imagePath, FileMode.CreateNew))
            {
                await uploadedImage.CopyToAsync(stream);
            }

            item.ImageUrl = $"/images/{imageFileName}";
            _context.FoodItems.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction("ManageMenu");
        }

        private async Task DeleteUploadedImageIfUnused(string imageUrl)
        {
            var imageFileName = Path.GetFileName(imageUrl);
            if (!imageUrl.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
                !Guid.TryParseExact(Path.GetFileNameWithoutExtension(imageFileName), "N", out _) ||
                await _context.FoodItems.AnyAsync(foodItem => foodItem.ImageUrl == imageUrl))
            {
                return;
            }

            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", imageFileName);
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        public IActionResult Orders(string status, string searchString, DateTime? fromDate, DateTime? toDate, string sortOrder)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // Store current sort order for view
            ViewBag.CurrentSort = sortOrder;                                                    //salvo ordinamento attivo
            ViewBag.DateSortParam = sortOrder == "date_asc" ? "date_desc" : "date_asc";         //do ordinamento
            ViewBag.TotalSortParam = sortOrder == "total_asc" ? "total_desc" : "total_asc";
            ViewBag.StatusSortParam = sortOrder == "status" ? "status_desc" : "status";

            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .AsQueryable();                     //per poter aggiungere ulteriori istruzioni alla query in seguito (ad es di filtraggio e ordinamento)

            // Filter by Status
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                orders = orders.Where(o => o.Status == status);
                ViewBag.CurrentStatus = status;
            }

            // Filter by Customer Name
            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o =>
                    o.User.FullName.Contains(searchString) ||
                    o.User.Username.Contains(searchString) ||
                    o.User.Email.Contains(searchString));
                ViewBag.CurrentSearch = searchString;
            }

            // Filter by Date Range
            if (fromDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= fromDate.Value);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }
            if (toDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= toDate.Value.AddDays(1));
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }

            // Sorting
            orders = sortOrder switch
            {
                "date_asc" => orders.OrderBy(o => o.OrderDate),
                "date_desc" => orders.OrderByDescending(o => o.OrderDate),
                "total_asc" => orders.OrderBy(o => o.TotalAmount),
                "total_desc" => orders.OrderByDescending(o => o.TotalAmount),
                "status" => orders.OrderBy(o => o.Status),
                "status_desc" => orders.OrderByDescending(o => o.Status),
                _ => orders.OrderByDescending(o => o.OrderDate) // default
            };

            // Status counts for dashboard stats
            ViewBag.PendingCount = _context.Orders.Count(o => o.Status == "Pending");
            ViewBag.ConfirmedCount = _context.Orders.Count(o => o.Status == "Confirmed");
            ViewBag.TodayCount = _context.Orders.Count(o => o.OrderDate.Date == DateTime.Today);
            ViewBag.TotalRevenue = _context.Orders.Where(o => o.Status != "Cancelled").Sum(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.StatusList = new List<string> { "All", "Pending", "Confirmed", "Preparing", "OutForDelivery", "Delivered", "Cancelled" };

            return View(orders.ToList());
        }

        public IActionResult OrderDetails(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");       //rimanda a Homecontroller, actionResult Index, se non sono admin

            var order = _context.Orders
                .Include(o => o.User)                                       //uso navigation property per includere i dettagli dell'utente associato all'ordine (x ogni ordine voglio vedere anche i dettagli dell'utente che l'ha fatto)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return NotFound();

            //ritorno view 
            return View("~/Views/Order/OrderDetails.cshtml", order);
        }
    }
}
