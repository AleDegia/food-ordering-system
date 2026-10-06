using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Services;

public interface ICartService
{
    //List<CartItem> GetItems(int userId);
    Task AddItem(ApplicationUser user, int foodItemId, int quantity);
    List<CartItem> GetItems(int userId);
    void UpdateQuantity(int userId, int foodItemId, int quantity);
    void RemoveItemFromCart(int userId, int foodItemId);
    void ClearCart(int userId);
    decimal GetTotal(int userId);
    int GetCount(int userId);
    Task<Order?> CreateOrderFromCartAsync(int userId,string deliveryAddress,string phoneNumber);
}