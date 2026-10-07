using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.ViewModels;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace FoodOrderingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly SessionCartService _sessionCartService;

        public AccountController(
         UserManager<ApplicationUser> userManager,
         SignInManager<ApplicationUser> signInManager,
         ApplicationDbContext context,
         SessionCartService sessionCartService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _sessionCartService = sessionCartService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();          //è come se passassi: return View(null);  (cerca Views/Account/Register.cshtml )
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Prima di entrare qui, ASP.NET ha già:
            // 1. creato il RegisterViewModel
            // 2. copiato i dati del form nelle proprietà
            // 3. eseguito le validazioni
            // 4. costruito il ModelState -> IsValid è false quando uno o più dati ricevuti dal form non rispettano le regole di validazione del model/ViewModel.
            if (ModelState.IsValid)         
            {
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    // password con hash
                    FullName = model.FullName,
                    Address = model.Address,
                    PhoneNumber = model.Phone
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }
                /*questo lo fa già _userManager.CreateAsync(user, model.Password) con la logica di Identity*/
                //_context.Users.Add(user);
                //_context.SaveChanges();

                return RedirectToAction("Login");       //login page
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();                      // searches your project folders for Views/Account/Login.cshtml.
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Username,
                           model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                //OrderController controller = new OrderController();
                var sessionCart = _sessionCartService.GetCart();
                int userId = Convert.ToInt32(_userManager.GetUserId(User));

                var loggedCart = _context.Carts
                    .FirstOrDefault(c => c.UserId == userId);

                if (loggedCart == null)
                {
                    loggedCart = new Cart { UserId = userId };
                    _context.Carts.Add(loggedCart);
                    _context.SaveChanges();
                }
                var existingItem = _context.CartItems.FirstOrDefault(i =>
                      i.CartId == loggedCart.Id);
                foreach (var sessionItem in sessionCart)
                {
                    if (existingItem != null && existingItem.FoodItemId == sessionItem.FoodItemId)
                    {
                        existingItem.Quantity +=
                        sessionItem.Quantity;
                    }
                    else
                    {
                        _context.CartItems.Add(new CartItem
                        {
                            CartId = loggedCart.Id,
                            FoodItemId = sessionItem.FoodItemId,
                            Quantity = sessionItem.Quantity
                        });
                    }
                }
                _context.SaveChanges();
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Username o password non validi.");
            return View(model);
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);                    // utente autenticato presente nella richiesta/cookie dentro ogni controller MVC
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Address = user.Address,
                Phone = user.PhoneNumber
            };

            return View(model);
        }


        [Authorize]
        [HttpPost]          //al submit del form del profilo
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (ModelState.IsValid)
            {
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.PhoneNumber = model.Phone;
                user.Address = model.Address;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");                         //manda all'action Profile() qui sopra
            }

            return View(model);                                             //se il ModelState non è valido La view riceve il ModelState con gli errori e li mostra automaticamente
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Impossibile eliminare l'account.";
                return RedirectToAction("Profile");
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (ModelState.IsValid)
            {
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("CurrentPassword", "La tua password non è questa");
                    return View(model);
                }

                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("Profile");
            }
            return View(model);                                         //se il ModelState non è valido La view riceve il ModelState con gli errori e li mostra automaticamente
        }
    }
}
