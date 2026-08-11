using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
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

        public IActionResult ManageMenu(int? categoryId, string searchString, bool? isAvailable, decimal? minPrice, decimal? maxPrice, string sortOrder)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            // Store sort order for view
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParam = sortOrder == "name_desc" ? "name" : "name_desc";
            ViewBag.PriceSortParam = sortOrder == "price_asc" ? "price_desc" : "price_asc";
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

            items = sortOrder switch
            {
                "name" => items.OrderBy(f => f.Name),
                "name_desc" => items.OrderByDescending(f => f.Name),

                "price_asc" => items.OrderBy(f => f.Price),
                "price_desc" => items.OrderByDescending(f => f.Price),

                "category" => items.OrderBy(f => f.Category.Name),
                "category_desc" => items.OrderByDescending(f => f.Category.Name),

                _ => items.OrderBy(f => f.Name)
            };

            ViewBag.Categories = _context.Categories.ToList();

            return View(items.ToList());
        }
    }
}
