using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
         UserManager<ApplicationUser> userManager,
         SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();          //è come se passassi: return View(null);  (cerca Views/Account/Register.cshtml )
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Prima di entrare qui, ASP.NET ha già:
            // 1. creato il RegisterViewModel
            // 2. copiato i dati del form nelle proprietà
            // 3. eseguito le validazioni
            // 4. costruito il ModelState
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
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Username,
                           model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
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


        [HttpPost]          //al submit del form del profilo
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

        //[HttpPost]          
        //public IActionResult DeleteAccount()
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");
        //    if (userId == null) return RedirectToAction("Login");

        //    var user = _context.Users.Find(userId);
        //    if (user == null) return NotFound();

        //    _context.Remove(user);
        //    _context.SaveChanges();
        //    HttpContext.Session.Clear();

        //    TempData["Success"] = "Profile removed successfully!";
        //    return RedirectToAction("Login", "Account");                         
        //}

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "Logout successful!";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        //[HttpPost]
        //public IActionResult ChangePassword(ChangePasswordViewModel model)
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");
        //    if (userId == null) return RedirectToAction("Login");

        //    if (ModelState.IsValid)
        //    {
        //        var user = _context.Users.Find(userId);                 //EF trova e crea user e lo registra nel changeTracker
        //        if(user == null) return NotFound();

        //        if (_passwordHasher.VerifyHashedPassword(
        //              user,
        //              user.Password,
        //              model.CurrentPassword) == PasswordVerificationResult.Failed)
        //        {           
        //            ModelState.AddModelError("CurrentPassword", "La tua password non è questa");
        //            return View(model); 
        //        }

        //        user.Password = model.NewPassword;                      // In production, hash the password!
        //        _context.SaveChanges();                                   

        //        TempData["Success"] = "Password changed successfully!";
        //        return RedirectToAction("Profile");
        //    }
        //    return View(model);                                         //se il ModelState non è valido La view riceve il ModelState con gli errori e li mostra automaticamente
        //}
    }
}
