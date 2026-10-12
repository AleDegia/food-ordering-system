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


    [Fact]
    public void UpdateQuantity_UpdatesOnlyTheCurrentUsersItem()
    {
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
            Quantity = 1
        };

        database.Context.AddRange(itemA, itemB);
        database.Context.SaveChanges();

        var service = new CartService(database.Context);
        service.UpdateQuantity(userA.Id, food.Id, 5);

        //Assert
        Assert.Equal(5, database.Context.CartItems.Find(itemA.Id)!.Quantity);
        Assert.Equal(1, database.Context.CartItems.Find(itemB.Id)!.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateQuantity_WhenQuantityIsZeroOrNegative_DoesNotChangeCartItem(
     int invalidQuantity)
    {
        // Arrange
        using var database = new TestDatabase();

        var user = new ApplicationUser
        {
            Id = 1,
            UserName = "utente-a",
            Email = "a@test.it"
        };

        var food = database.Context.FoodItems.Find(1)!;
        var cart = new Cart { UserId = user.Id };

        database.Context.AddRange(user, cart);
        database.Context.SaveChanges();

        var item = new CartItem
        {
            CartId = cart.Id,
            FoodItemId = food.Id,
            Quantity = 2
        };

        database.Context.CartItems.Add(item);
        database.Context.SaveChanges();

        var service = new CartService(database.Context);

        // Act
        service.UpdateQuantity(user.Id, food.Id, invalidQuantity);

        // Assert
        var unchangedItem = database.Context.CartItems.Find(item.Id);

        Assert.NotNull(unchangedItem);                  //Verifica che l’articolo esista ancora
        Assert.Equal(2, unchangedItem.Quantity);
    }

    [Fact]
    public void ClearCart_RemovesOnlyTheCurrentUsersItems()
    {
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

        var cartA = new Cart { UserId = userA.Id };
        var cartB = new Cart { UserId = userB.Id };
        database.Context.AddRange(userA, userB, cartA, cartB);
        database.Context.SaveChanges();

        database.Context.AddRange(
            new CartItem { CartId = cartA.Id, FoodItemId = 1, Quantity = 2 },
            new CartItem { CartId = cartA.Id, FoodItemId = 2, Quantity = 1 },
            new CartItem { CartId = cartB.Id, FoodItemId = 3, Quantity = 4 });
        database.Context.SaveChanges();

        var service = new CartService(database.Context);

        service.ClearCart(userA.Id);

        Assert.Empty(database.Context.CartItems.Where(i => i.CartId == cartA.Id));
        var remainingItem = Assert.Single(
            database.Context.CartItems.Where(i => i.CartId == cartB.Id));
        Assert.Equal(3, remainingItem.FoodItemId);
        Assert.Equal(4, remainingItem.Quantity);
    }

    [Fact]
    public void GetTotal_ReturnsTheSumOfEachItemPriceXQuantity()
    {
        using var database = new TestDatabase();
        var user = new ApplicationUser
        {
            Id = 1,
            UserName = "utente-a",
            Email = "a@test.it"
        };
        var cart = new Cart { UserId = user.Id };
        database.Context.AddRange(user, cart);
        database.Context.SaveChanges();

        var margherita = database.Context.FoodItems.Find(1)!;
        var cheeseburger = database.Context.FoodItems.Find(3)!;
        database.Context.AddRange(
            new CartItem { CartId = cart.Id, FoodItemId = margherita.Id, Quantity = 2 },
            new CartItem { CartId = cart.Id, FoodItemId = cheeseburger.Id, Quantity = 3 });
        database.Context.SaveChanges();

        var service = new CartService(database.Context);

        var total = service.GetTotal(user.Id);

        var expectedTotal = (margherita.Price * 2) + (cheeseburger.Price * 3);
        Assert.Equal(expectedTotal, total);
    }




    [Fact]
    public async Task AddItem_WhenCartDoesNotExist_CreatesCartAndAddsItem()
    {
        using var database = new TestDatabase();

        var user = new ApplicationUser
        {
            Id = 1,
            UserName = "utente-a",
            Email = "a@test.it"
        };

        var food = database.Context.FoodItems.Find(1)!;
        database.Context.Users.Add(user);
        database.Context.SaveChanges();

        var service = new CartService(database.Context);

        await service.AddItem(user, food.Id, 2);

        var cart = Assert.Single(database.Context.Carts.Where(c => c.UserId == user.Id));
        var item = Assert.Single(database.Context.CartItems.Where(i => i.CartId == cart.Id));

        Assert.Equal(food.Id, item.FoodItemId);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task AddItem_WhenItemAlreadyExists_IncreasesQuantity()
    {
        using var database = new TestDatabase();
        var user = new ApplicationUser
        {
            Id = 1,
            UserName = "utente-a",
            Email = "a@test.it"
        };

        var food = database.Context.FoodItems.Find(1)!;
        database.Context.Users.Add(user);
        database.Context.SaveChanges();

        var service = new CartService(database.Context);

        await service.AddItem(user, food.Id, 2);
        await service.AddItem(user, food.Id, 3);

        var cart = Assert.Single(database.Context.Carts.Where(c => c.UserId == user.Id));
        var item = Assert.Single(database.Context.CartItems.Where(i => i.CartId == cart.Id));

        Assert.Equal(food.Id, item.FoodItemId);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public async Task AddItem_AddsItemOnlyToTheCurrentUsersCart()
    {
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

        var food = database.Context.FoodItems.Find(1)!;
        var cartA = new Cart { UserId = userA.Id };
        var cartB = new Cart { UserId = userB.Id };

        database.Context.AddRange(userA, userB, cartA, cartB);
        database.Context.SaveChanges();

        var service = new CartService(database.Context);

        await service.AddItem(userA, food.Id, 2);

        var itemForUserA = Assert.Single(
            database.Context.CartItems.Where(i => i.CartId == cartA.Id));
        Assert.Equal(food.Id, itemForUserA.FoodItemId);
        Assert.Equal(2, itemForUserA.Quantity);
        Assert.Empty(database.Context.CartItems.Where(i => i.CartId == cartB.Id));
    }
}
