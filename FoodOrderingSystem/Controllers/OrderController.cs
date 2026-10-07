
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.NetworkInformation;

namespace FoodOrderingSystem.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICartService _cartService;
        private readonly SessionCartService _sessionCartService;
        private readonly IOrderService _orderService;
        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ICartService cartService, SessionCartService sessionCartService, IOrderService orderService)
        {
            _context = context;
            _userManager = userManager;
            _cartService = cartService;
            _sessionCartService = sessionCartService;
            _orderService = orderService;
        }


        //Ogni volta che un utente clicca su "Add to Cart", ASP.NET Core esegue questo metodo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int foodItemId, int quantity = 1)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                var filledCart = _sessionCartService.AddItem(foodItemId, quantity);
                SaveCart(filledCart);
            }
            else
            {
                var user = await _userManager.GetUserAsync(User);
                await _cartService.AddItem(user, foodItemId, quantity);
            }
            return Redirect(Request.Headers["Referer"].ToString());
        }

        //quando clicco sul carrello
        public IActionResult Cart()
        {
            List<SessionCartItem> cart;
            if (!User.Identity?.IsAuthenticated == true)
            {
                // Retrieve items from session
                cart = _sessionCartService.GetCart();
            }
            else
            {
                int userId = Convert.ToInt32(_userManager.GetUserId(User));
                var items = _cartService.GetItems(userId);

                cart =
                items.Select(i => new SessionCartItem
                {
                    FoodItemId = i.FoodItemId,
                    Name = i.FoodItem.Name,
                    Price = i.FoodItem.Price,
                    Quantity = i.Quantity,
                    ImageUrl = i.FoodItem.ImageUrl
                }).ToList();
            }
            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
            // Pass the list to the View
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int foodItemId, int quantity)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                _sessionCartService.UpdateQuantity(foodItemId, quantity);
            }
            else
            {
                int userId = Convert.ToInt32(_userManager.GetUserId(User));
                _cartService.UpdateQuantity(userId, foodItemId, quantity);
            }
            return RedirectToAction("Cart");        //faccio richiesta HTTP all'action 'Cart'
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int foodItemId)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                _sessionCartService.RemoveFromCart(foodItemId);
            } 
            else
            {
                int userId = Convert.ToInt32(_userManager.GetUserId(User));
                _cartService.RemoveItemFromCart(userId, foodItemId);
            }
            return RedirectToAction("Cart");
        }
                    
        public List<SessionCartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");           //cerca chiave cart nella sessione e ne prende il valore
            return cartJson == null ? new List<SessionCartItem>() :                //se non la trova (se è null) cra nuovo oggetto lista di tipo CartItem
                JsonConvert.DeserializeObject<List<SessionCartItem>>(cartJson);
        }

        private void SaveCart(List<SessionCartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
        }

        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                var cart = _sessionCartService.GetCart();
                if (!cart.Any()) return RedirectToAction("Index", "Menu");

                //ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
                RedirectToAction("Login", "Account");
                return View();
            }
            else
            {
                int userId = Convert.ToInt32(_userManager.GetUserId(User));
                ViewBag.Total = _cartService.GetTotal(userId);
                return View();
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string deliveryAddress, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return RedirectToAction("Login", "Account");

            //devo mantenere gli item nel carrello dell'utente non loggato se logga
            int userId = user.Id;
            var order = await _cartService.CreateOrderFromCartAsync(userId, deliveryAddress, phoneNumber);

            if (order == null)
                return RedirectToAction("Cart");

            HttpContext.Session.Remove("Cart");
            //_cartService.ClearCart(userId);          
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });           //reindirizzo all'action passandogli il parametro
        }

        public IActionResult OrderConfirmation(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        [Authorize]
        //filtri (prendo parametri dagli input del form col name uguale al nome parametro che do qui) 
        public async Task<IActionResult> MyOrders(string status, string sortOrder, DateTime? fromDate, DateTime? toDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return RedirectToAction("Login", "Account");

            int userId = user.Id;
            var orders = _orderService.GetMyOrders(status, sortOrder, fromDate, toDate, userId);

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                ViewBag.CurrentStatus = status;
            }

            if (fromDate.HasValue)
            {
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }
            if (toDate.HasValue)
            {
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }

            ViewBag.CurrentSort = sortOrder;
            ViewBag.StatusList = new List<string> { "All", "Pending", "Confirmed", "Preparing", "OutForDelivery", "Delivered", "Cancelled" };

            return View(orders);
        }

        [Authorize]
        public async Task<IActionResult> TrackOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return RedirectToAction("Login", "Account");

            int userId = user.Id;

            var order = _orderService.GetUserOrderDetails(id, userId);

            if (order == null) return NotFound();

            return View(order);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMyOrder(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return RedirectToAction("Login", "Account");

            var result = _orderService.CancelUserOrder(orderId, user.Id);

            if (result.Code == OperationResultCode.NotFound)
                return NotFound();

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction("TrackOrder", new { id = orderId });
            }

            TempData["Success"] = "Order cancelled successfully!";
            return RedirectToAction("MyOrders");
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            // Retrieve the Cart JSON string from the Session
            int count;
            if(User.Identity?.IsAuthenticated == true)
            {
                int userId = Convert.ToInt32(_userManager.GetUserId(User));
                count = _cartService.GetCount(userId);
            }
            else
            {
                count = _sessionCartService.GetCount();
            }

            // Return the count as a JSON object for AJAX calls
            return Json(new { count });
        }
    }

    public class SessionCartItem
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
    }
}
