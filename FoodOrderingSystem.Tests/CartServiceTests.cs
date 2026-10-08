using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;

namespace FoodOrderingSystem.Tests;

public class CartServiceTests
{
    [Fact]
    public void RemoveItemFromCart_RemovesOnlyTheCurrentUsersItem()
    {
        // Arrange: creo database e dati iniziali
        using var database = new TestDatabase();

        var userA = new ApplicationUser
        {
            Id = 1,
            UserName = "utente-a",
            Email = "a@test.it"
        };

        var userB = new ApplicationUser
        {
            Id = 2,
            UserName = "utente-b",
            Email = "b@test.it"
        };

        // ApplicationDbContext seeds FoodItem with Id = 1 during EnsureCreated().
        var food = database.Context.FoodItems.Find(1)!;

        var cartA = new Cart { UserId = userA.Id };
        var cartB = new Cart { UserId = userB.Id };

        database.Context.AddRange(userA, userB, cartA, cartB);
        database.Context.SaveChanges();

        var itemA = new CartItem
        {
            CartId = cartA.Id,
            FoodItemId = food.Id,
            Quantity = 1
        };

        var itemB = new CartItem
        {
            CartId = cartB.Id,
            FoodItemId = food.Id,
            Quantity = 2
        };

        database.Context.AddRange(itemA, itemB);
        database.Context.SaveChanges();

        var service = new CartService(database.Context);

        // Act: utente A rimuove la propria pizza
        service.RemoveItemFromCart(userA.Id, food.Id);

        // Assert
        Assert.Empty(database.Context.CartItems.Where(i => i.CartId == cartA.Id));

        var remainingItem = Assert.Single(
              database.Context.CartItems.Where(i => i.CartId == cartB.Id));

        Assert.Equal(2, remainingItem.Quantity);
    }
}
