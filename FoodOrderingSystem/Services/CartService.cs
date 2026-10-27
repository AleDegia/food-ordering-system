using AspNetCoreGeneratedDocument;
using FoodOrderingSystem.Controllers;
using FoodOrderingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FoodOrderingSystem.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<OperationResult> AddItem(ApplicationUser user, int foodItemId, int quantity)
        {
            var cart = _context.Carts
                    .Include(c => c.Items)
                    .FirstOrDefault(c => c.UserId == user.Id);
            var foodItem = await _context.FoodItems.FindAsync(foodItemId);

            if (foodItem is null)
                return OperationResult.NotFound("Food item not found.");

            if (!foodItem.IsAvailable)
                return OperationResult.OutOfStock("This item is currently unavailable.");

            if (quantity <= 0)
                return OperationResult.InvalidRequest("Quantity must be at least 1.");

            if (cart != null)
            {
                //controllo se esiste già un item con lo stesso foodItemId nel carrello dell'utente loggato
                var existingItem = cart.Items.FirstOrDefault(i => i.FoodItemId == foodItemId);

                var requestedQuantity = (existingItem?.Quantity ?? 0) + quantity;
                if (foodItem.StockQuantity < requestedQuantity)
                {
                    return OperationResult.OutOfStock(
                        $"Only {foodItem.StockQuantity} item(s) are available.");
                }

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
                if (foodItem.StockQuantity < quantity)
                {
                    return OperationResult.OutOfStock(
                        $"Only {foodItem.StockQuantity} item(s) are available.");
                }

                cart = new Cart
                {
                    UserId = user.Id
                };

                cart.Items.Add(new CartItem
                {
                    FoodItemId = foodItemId,
                    Quantity = quantity,
                });
                

                // Il carrello è nuovo: solo in questo caso va aggiunto al contesto.
                _context.Carts.Add(cart);
            }
            await _context.SaveChangesAsync();
            return OperationResult.Success();
        }


        public List<CartItem> GetItems(int userId)
        {
            return _context.CartItems
                .Include(i => i.FoodItem)
                .Where(i => i.Cart.UserId == userId)
                .ToList();
        }

        public async Task<Order?> CreateOrderFromCartAsync(
            int userId,
            string deliveryAddress,
            string phoneNumber)
        {
            List<CartItem> cartItems = GetItems(userId);

            if (!cartItems.Any())
                return null;

            var order = new Order
            {
                UserId = userId,
                DeliveryAddress = deliveryAddress,
                PhoneNumber = phoneNumber,
                TotalAmount = cartItems.Sum(i => i.FoodItem.Price * i.Quantity),
                OrderItems = cartItems.Select(i => new OrderItem
                {
                    FoodItemId = i.FoodItemId,
                    Quantity = i.Quantity,
                    UnitPrice = i.FoodItem.Price
                }).ToList()
            };

            foreach (var cartItem in cartItems)
            {
                var foodItem = cartItem.FoodItem;

                if (foodItem is null ||
                    !foodItem.IsAvailable ||
                    cartItem.Quantity <= 0 ||
                    foodItem.StockQuantity < cartItem.Quantity)
                {
                    return null;                                            
                }
            }

            // Solo dopo aver verificato tutti i prodotti:
            foreach (var cartItem in cartItems)
            {
                cartItem.FoodItem.StockQuantity -= cartItem.Quantity;
            }


            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();                                                                       //salva anche la modifica a FoodItem, insieme all’ordine e alla rimozione dei CartItem
            return order;
        }



        public OperationResult UpdateQuantity(int userId, int foodItemId, int quantity)
        {
            if (quantity <= 0)
                return OperationResult.InvalidRequest("Quantity must be at least 1."); // non faccio nemmeno la query al DB
            
            var foodItem = _context.FoodItems.FirstOrDefault(f => f.Id == foodItemId);

            if (foodItem is null ||
                !foodItem.IsAvailable ||
                foodItem.StockQuantity < quantity)
            {
                return OperationResult.OutOfStock("This item is currently unavailable or we do not have enouth stock.");
            }

            var item = _context.CartItems.FirstOrDefault(i =>
                i.FoodItemId == foodItemId && i.Cart.UserId == userId);
            if (item != null)
            {
                item.Quantity = quantity;
                _context.SaveChanges();
            }
            return OperationResult.Success();
        }


        public void RemoveItemFromCart(int userId, int foodItemId)
        {
            var item = _context.CartItems.FirstOrDefault(i =>
                i.FoodItemId == foodItemId && i.Cart.UserId == userId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                _context.SaveChanges();
            }
        }

        public void ClearCart(int userId)
        {
            /*var itemsToRemove = _context.CartItems.Where(i => userId == i.Cart.UserId).ToList();
            _context.CartItems.RemoveRange(itemsToRemove);*/
            var cart = _context.Carts.Include(c => c.Items).FirstOrDefault(c => c.UserId == userId);
            if (cart != null)
            {
                _context.CartItems.RemoveRange(cart.Items);
                _context.SaveChanges();
            }
        }

        public decimal GetTotal(int userId)
        {
            return _context.CartItems
                .Where(i => i.Cart.UserId == userId)
                .Sum(i => i.Quantity * i.FoodItem.Price);
        }

        public int GetCount(int userId)
        {
            return _context.CartItems
                .Where(i => i.Cart.UserId == userId)
                .Sum(i => i.Quantity);
        }
    }
}
