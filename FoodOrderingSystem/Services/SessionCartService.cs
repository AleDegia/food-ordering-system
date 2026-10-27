using FoodOrderingSystem.Controllers;
using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FoodOrderingSystem.Services
{
    public class SessionCartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;


        public SessionCartService(IHttpContextAccessor
        httpContextAccessor, ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public List<SessionCartItem> GetCart()
        {
            var session = _httpContextAccessor.HttpContext!.Session;
            var cartJson = session.GetString("Cart");

            return string.IsNullOrEmpty(cartJson)
              ? new List<SessionCartItem>()
              :
              JsonConvert.DeserializeObject<List<SessionCartItem>>(cartJson)
                ?? new List<SessionCartItem>();
        }

        private void SaveCart(List<SessionCartItem> cart)
        {
            var session = _httpContextAccessor.HttpContext!.Session;
            session.SetString("Cart", JsonConvert.SerializeObject(cart));
        }

        public OperationResult AddItem(int foodItemId, int quantity)
        {
            var foodItem = _context.FoodItems.Find(foodItemId);
            if (foodItem is null)
                return OperationResult.NotFound("Food item not found.");

            if (!foodItem.IsAvailable)
                return OperationResult.OutOfStock("This item is currently unavailable.");

            if (quantity <= 0)
                return OperationResult.InvalidRequest("Quantity must be at least 1.");

            List<SessionCartItem> cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);
            var requestedQuantity = (existingItem?.Quantity ?? 0) + quantity;

            if (foodItem.StockQuantity < requestedQuantity)
            {
                return OperationResult.OutOfStock(
                    $"Only {foodItem.StockQuantity} item(s) are available.");
            }

            //se prodotto c'è gia nel carrello aggiunge 1 ogni volta che lo riaggiungo al carrello
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                //trovo foodItem e lo aggiungo a cart sottoforma di CartItem
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
            return OperationResult.Success();
        }


        public void UpdateQuantity(int foodItemId, int quantity)
        {
            if (quantity <= 0)
                return;

            var foodItem = _context.FoodItems.FirstOrDefault(f => f.Id == foodItemId);

            if (foodItem is null ||
                !foodItem.IsAvailable ||
                foodItem.StockQuantity < quantity)
            {
                return;
            }

            List<SessionCartItem> cart = GetCart();
            var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);
            }
        }


        public void RemoveFromCart(int foodItemId)
        {

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.FoodItemId == foodItemId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
        }

        public int GetCount()
        {
            return GetCart().Sum(item => item.Quantity);
        }
    }
}
