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
    }
}
