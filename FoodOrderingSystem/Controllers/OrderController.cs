
using FoodOrderingSystem.Models;
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

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        //Ogni volta che un utente clicca su "Add to Cart", ASP.NET Core esegue questo metodo.
        public async Task<IActionResult> AddToCart(int foodItemId, int quantity = 1)
        
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                List<SessionCartItem> cart = GetCart();
                var existingItem = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

                //se prodotto c'è gia nel carrello aggiunge 1 ogni volta che lo riaggiungo al carrello
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    //trovo foodItem e lo aggiungo a cart sottoforma di CartItem
                    var foodItem = _context.FoodItems.Find(foodItemId);
                    cart.Add(new SessionCartItem
                    {
                        FoodItemId = foodItemId,
                        Name = foodItem.Name,
                        Price = foodItem.Price,
                        Quantity = quantity,
                        ImageUrl = foodItem.ImageUrl
                    });
                }

                SaveCart(cart);
            }
            else
            {
                var user = await _userManager.GetUserAsync(User);
                var cart = _context.Carts
                    .Include(c => c.Items)
                    .FirstOrDefault(c => c.UserId == user.Id);
                if (cart != null)
                {
                    //controllo se esiste già un item con lo stesso foodItemId nel carrello dell'utente loggato
                    var existingItem = cart.Items.FirstOrDefault(i => i.FoodItemId == foodItemId);
                    if (existingItem != null)
                    {
                        existingItem.Quantity += quantity;
                    }
                    else 
                    {
                        cart.Items.Add(new CartItem                     //items valorizzato da Include(c => c.Items) sopra, quindi posso aggiungere un nuovo item al carrello dell'utente loggato
                        {
                            FoodItemId = foodItemId,
                            Quantity = quantity
                        });
                    }
                }
                else
                {
                    cart = new Cart
                    {
                        UserId = user.Id
                    };

                    cart.Items.Add(new CartItem
                    {
                        FoodItemId = foodItemId,
                        Quantity = quantity
                    });

                    // Il carrello è nuovo: solo in questo caso va aggiunto al contesto.
                    _context.Carts.Add(cart);
                }
                await _context.SaveChangesAsync();
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
                cart = GetCart();
            }
            else
            {
                List<CartItem> items = _context.CartItems
                   .Include(i => i.FoodItem)
                   .Where(i => i.Cart.UserId == Convert.ToInt32(_userManager.GetUserId(User)))
                   .ToList();

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
            // calcolo totale e lo passo a parte con ViewBag
            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
            // Pass the list to the View
            return View(cart);
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int foodItemId, int quantity)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                List<SessionCartItem> cart = GetCart();
                var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

                if (item != null && quantity > 0)
                {
                    item.Quantity = quantity;
                    SaveCart(cart);
                }
            }
            else
            {
                var item = _context.CartItems.FirstOrDefault(i => i.FoodItem.Id == foodItemId && i.Cart.UserId == Convert.ToInt32(_userManager.GetUserId(User)));
                item.Quantity = quantity;
                _context.SaveChanges();
            }
            return RedirectToAction("Cart");        //faccio richiesta HTTP all'action 'Cart'
        }

        public IActionResult RemoveFromCart(int foodItemId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
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
                var cart = GetCart();
                if (!cart.Any()) return RedirectToAction("Index", "Menu");

                //ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
                RedirectToAction("Login", "Account");
                return View();
            }
            else
            {
                int userId = Convert.ToInt32(_userManager.GetUserId(User));
                ViewBag.Total = _context.CartItems
                    .Where(i=>i.Cart.UserId == userId)
                    .Sum(i => i.Quantity * i.FoodItem.Price);
                return View();
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(string deliveryAddress, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return RedirectToAction("Login", "Account");

            //devo mantenere gli item nel carrello dell'utente non loggato se logga
            int userId = user.Id;
            var cart = GetCart();
            if (user != null)
            {
               Cart? loggedCart = _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.FoodItem)
                    .FirstOrDefault(c => c.UserId == userId);
               cart = loggedCart?.Items.Select(i => new SessionCartItem
               {
                   FoodItemId = i.FoodItemId,
                   Name = i.FoodItem.Name,
                   Price = i.FoodItem.Price,
                   Quantity = i.Quantity,
                   ImageUrl = i.FoodItem.ImageUrl
               }).ToList() ?? new List<SessionCartItem>();
            }

            var order = new Order
            {
                UserId = userId,                                  //metto .Value perchè userId è nullable, UserId no e nonpuò prendere null come valore.
                DeliveryAddress = deliveryAddress,
                PhoneNumber = phoneNumber,
                TotalAmount = cart.Sum(c => c.Price * c.Quantity),
                OrderItems = cart.Select(c => new OrderItem             //Select è un LINQ che trasforma ogni CartItem della lista in un nuovo OrderItem
                {
                    FoodItemId = c.FoodItemId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                }).ToList()                                             //trasformo in lista di OrderItem
            };

            _context.Orders.Add(order);
            
            HttpContext.Session.Remove("Cart");
            var itemsToRemove = _context.CartItems.Where(i => userId == i.Cart.UserId).ToList();
            _context.CartItems.RemoveRange(itemsToRemove);
            _context.SaveChanges();    
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

            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            // Filter by Status
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                orders = orders.Where(o => o.Status == status);
                ViewBag.CurrentStatus = status;
            }

            // Filter by Date Range
            if (fromDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= fromDate.Value);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }
            if (toDate.HasValue)
            {
                // Add 1 day to include the entire 'To' date
                orders = orders.Where(o => o.OrderDate <= toDate.Value.AddDays(1));
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }

            // Sorting Logic (mando a frontend la lista degli ordini gia ordinata)
            ViewBag.CurrentSort = sortOrder;
            orders = sortOrder switch
            {
                "date_asc" => orders.OrderBy(o => o.OrderDate),
                "total_desc" => orders.OrderByDescending(o => o.TotalAmount),
                "total_asc" => orders.OrderBy(o => o.TotalAmount),
                _ => orders.OrderByDescending(o => o.OrderDate) // _ è il default dello switch compatto: newest first
            };

            ViewBag.StatusList = new List<string> { "All", "Pending", "Confirmed", "Preparing", "OutForDelivery", "Delivered", "Cancelled" };

            return View(orders.ToList());
        }

        [Authorize]
        public async Task<IActionResult> TrackOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return RedirectToAction("Login", "Account");

            int userId = user.Id;

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .FirstOrDefault(o => o.Id == id && o.UserId == userId);             //Esegue

            if (order == null) return NotFound();

            return View(order);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelMyOrder(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return RedirectToAction("Login", "Account");

            int userId = user.Id;
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return NotFound();

            // Only allow cancellation if status is Pending or Confirmed
            if (order.Status != "Pending" && order.Status != "Confirmed")
            {
                TempData["Error"] = "Order cannot be cancelled at this stage!";
                return RedirectToAction("TrackOrder", new { id = orderId });
            }

            order.Status = "Cancelled";
            _context.SaveChanges();

            TempData["Success"] = "Order cancelled successfully!";
            return RedirectToAction("MyOrders");
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            // Retrieve the Cart JSON string from the Session
            var count = 0;
            if(!User.Identity?.IsAuthenticated == true)
            {
                var cartJson = HttpContext.Session.GetString("Cart");

                // Check if the cart is not empty
                if (!string.IsNullOrEmpty(cartJson))
                {
                    // Deserialize the JSON back into a List of CartItems
                    var cart = JsonConvert.DeserializeObject<List<CartItem>>(cartJson);

                    // Sum up the total quantity of all items in the cart
                    count = cart.Sum(c => c.Quantity);
                }
            }
            else
            {
                int userId = Convert.ToInt32(_userManager.GetUserId(User));
                if (userId == 0)
                    return Json(new { count = 0 });

                count = _context.CartItems
                  .Where(i => i.Cart.UserId == userId)
                  .Sum(i => (int?)i.Quantity) ?? 0;
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
