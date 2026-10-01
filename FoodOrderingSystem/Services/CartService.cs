using FoodOrderingSystem.Controllers;
using FoodOrderingSystem.Models;
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
                    Quantity = quantity
                });

                // Il carrello è nuovo: solo in questo caso va aggiunto al contesto.
                _context.Carts.Add(cart);
            }
            await _context.SaveChangesAsync();
        }
    }
}
