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
        public async Task AddItem(ApplicationUser user, int foodItemId, int quantity)
        {
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
                    Quantity = quantity,
                });

                // Il carrello è nuovo: solo in questo caso va aggiunto al contesto.
                _context.Carts.Add(cart);
            }
            await _context.SaveChangesAsync();
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

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            return order;
        }



        public void UpdateQuantity(int userId, int foodItemId, int quantity)
        {
            if (quantity <= 0)
                return; // non faccio nemmeno la query al DB

            var item = _context.CartItems.FirstOrDefault(i => i.FoodItem.Id == foodItemId && i.Cart.UserId == userId);
            if (item != null)
            {
                item.Quantity = quantity;
                _context.SaveChanges();
            }
        }


        public void RemoveItemFromCart(int userId, int foodItemId)
        {
            var item = _context.CartItems.FirstOrDefault(i => i.FoodItemId == foodItemId);
            if (item != null && userId != 0)
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
