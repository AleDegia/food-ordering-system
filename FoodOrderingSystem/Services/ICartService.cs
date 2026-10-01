using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Services;

public interface ICartService
{
    //List<CartItem> GetItems(int userId);
    Task AddItem(ApplicationUser user, int foodItemId, int quantity);
    //void UpdateQuantity(int userId, int
    //  foodItemId, int quantity);
    //void RemoveItem(int userId, int foodItemId);
    //void ClearCart(int userId);
}