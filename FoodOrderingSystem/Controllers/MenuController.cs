using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? categoryId )
        {
            ViewBag.Categories = _context.Categories.ToList();

            // recupero cibi disponibili + categoria
            var foodItems = _context.FoodItems
                .Include(f => f.Category)               //.Include non è Linq, ma EF
                .Where(f => f.IsAvailable)
                .ToList(); 
            
            if (categoryId.HasValue)                    //se ho categoryId (quindi se ho selezionato una categoria) filtro per quella           
            { 
                foodItems = foodItems.Where(f => f.CategoryId == categoryId.Value).ToList(); 
            }

            return View(foodItems);
        }
    }
}
