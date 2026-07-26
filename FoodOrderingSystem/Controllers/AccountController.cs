using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();          //è come se passassi: return View(null);  (cerca Views/Account/Register.cshtml )
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            // Prima di entrare qui, ASP.NET ha già:
            // 1. creato il RegisterViewModel
            // 2. copiato i dati del form nelle proprietà
            // 3. eseguito le validazioni
            // 4. costruito il ModelState
            if (ModelState.IsValid)         
            {
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = model.Password, // In production, hash this!
                    FullName = model.FullName,
                    Address = model.Address,
                    Phone = model.Phone
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("#");       //login page
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();                      // searches your project folders for Views/Account/Login.cshtml.
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            var user = _context.Users.FirstOrDefault(u =>
                u.Username == model.Username && u.Password == model.Password);

            if (user != null)               //serve a memorizzare informazioni dell'utente sulla sessione server dopo il login.
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid username or password");       //aggiunge errore
            return View(model);
        }


        [HttpGet]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = _context.Users.Find(userId);                             //trovo utente nel db grazie al suo id nella sessione
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Address = user.Address,
                Phone = user.Phone
            };

            return View(model);
        }


        [HttpPost]          //al submit del form del profilo
        public IActionResult Profile(ProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            if (ModelState.IsValid)
            {
                var user = _context.Users.Find(userId);
                if (user == null) return NotFound();

                // Check if email is already taken by another user
                var emailExists = _context.Users.Any(u => u.Email == model.Email && u.Id != userId);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email is already registered to another account.");       
                    return View(model);
                }

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.Address = model.Address;

                _context.SaveChanges();                                     //metodo di EF che genera ed esegue l’UPDATE nel database.
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");                         //manda all'action Profile() qui sopra
            }

            return View(model);
        }
    }
}
