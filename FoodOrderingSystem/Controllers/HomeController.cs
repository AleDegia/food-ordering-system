using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext context;

        public HomeController(ApplicationDbContext _context)
        {
            context = _context;
        }

        public IActionResult Index()
        {
            var fooditems = context.FoodItems
                .Where(f => f.IsAvailable)              //ritorna elementi che restituiscono true
                .Take(6)                                //prende i primi 6
                .ToList();                              //Esegue la query verso il database e Materializza i risultati in una List<FoodItem>.

            return View(fooditems);                     //passa la lista dei miei 6 item all'Index.cshtml
        }
    }
}