
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FoodOrderingSystem.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Ogni volta che un utente clicca su "Add to Cart", ASP.NET Core esegue questo metodo.
        public IActionResult AddToCart(int foodItemId, int quantity = 1)
        {
            List<CartItem> cart = GetCart();
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
                cart.Add(new CartItem
                {
                    FoodItemId = foodItemId,
                    Name = foodItem.Name,
                    Price = foodItem.Price,
                    Quantity = quantity,
                    ImageUrl = foodItem.ImageUrl
                });
            }

            SaveCart(cart);
            return Redirect(Request.Headers["Referer"].ToString());
        }

        //quando clicco sul carrello
        public IActionResult Cart()
        {
            // Retrieve items from session
            List<CartItem> cart = GetCart();

            // calcolo totale e lo passo a parte con ViewBag
            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);

            // Pass the list to the View
            return View(cart);
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int foodItemId, int quantity)
        {
            List<CartItem> cart = GetCart();
            var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

            if (item != null && quantity > 0)
            {
                item.Quantity = quantity;
                SaveCart(cart);
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

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");           //cerca chiave cart nella sessione e ne prende il valore
            return cartJson == null ? new List<CartItem>() :                //se non la trova (se è null) cra nuovo oggetto lista di tipo CartItem
                JsonConvert.DeserializeObject<List<CartItem>>(cartJson);
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index", "Menu");

            ViewBag.Total = cart.Sum(c => c.Price * c.Quantity);
            return View();
        }

        [HttpPost]
        public IActionResult Checkout(string deliveryAddress, string phoneNumber)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var cart = GetCart();

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = new Order
            {
                UserId = userId.Value,                                  //metto .Value perchè userId è nullable, UserId no e nonpuò prendere null come valore.
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
            _context.SaveChanges();

            HttpContext.Session.Remove("Cart");
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });           //reindirizzo all'action passandogli il parametro
        }

        public IActionResult OrderConfirmation(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        public IActionResult MyOrders()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            return View(orders.ToList());
        }

    }

    public class CartItem
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
    }
}