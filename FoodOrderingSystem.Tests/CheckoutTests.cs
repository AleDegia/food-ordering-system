using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Tests;

public class CheckoutTests
{
    [Fact]
    public async Task Checkout_CommitsStockOrderAndOnlyTheCurrentUsersCart()
    {
        using var database = new TestDatabase();
        var context = database.Context;
        var user = await SeedCartAsync(context);
        var otherUser = new ApplicationUser { UserName = "other-user" };
        context.Carts.Add(new Cart
        {
            User = otherUser,
            Items = [new CartItem { FoodItemId = 1, Quantity = 1 }]
        });
        await context.SaveChangesAsync();

        var order = await new CartService(context)
            .CreateOrderFromCartAsync(user.Id, "Via Roma 1", "3331234567");

        Assert.NotNull(order);
        context.ChangeTracker.Clear();
        Assert.Equal(3, (await context.FoodItems.FindAsync(1))!.StockQuantity);
        Assert.Equal(4, (await context.FoodItems.FindAsync(2))!.StockQuantity);
        var savedOrder = await context.Orders.Include(o => o.OrderItems).SingleAsync();
        Assert.Equal(user.Id, savedOrder.UserId);
        Assert.Equal(2, savedOrder.OrderItems.Count);
        var foods = await context.FoodItems.ToDictionaryAsync(f => f.Id);
        Assert.Equal(2 * foods[1].Price + foods[2].Price, savedOrder.TotalAmount);
        Assert.All(savedOrder.OrderItems, i => Assert.Equal(foods[i.FoodItemId].Price, i.UnitPrice));
        Assert.Empty(await context.CartItems.Where(i => i.Cart.UserId == user.Id).ToListAsync());
        Assert.Single(await context.CartItems.Where(i => i.Cart.UserId == otherUser.Id).ToListAsync());
        Assert.True(await context.Carts.AnyAsync(c => c.UserId == user.Id));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    [InlineData(5, false)]
    public async Task Checkout_WhenLaterProductFails_RollsBackEarlierDecrement(int stock, bool available)
    {
        using var database = new TestDatabase();
        await VerifyStockFailureAsync(database.Context, stock, available);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Checkout_WhenQuantityIsInvalid_LeavesDatabaseUnchanged(int quantity)
    {
        using var database = new TestDatabase();
        var context = database.Context;
        var user = await SeedCartAsync(context);
        var item = await context.CartItems.SingleAsync(i => i.FoodItemId == 2);
        item.Quantity = quantity;
        await context.SaveChangesAsync();

        var order = await new CartService(context)
            .CreateOrderFromCartAsync(user.Id, "Via Roma 1", "3331234567");

        Assert.Null(order);
        await AssertUnchangedAsync(context, 5);
    }

    [Fact]
    public async Task Checkout_WhenCartIsEmpty_DoesNotCreateOrder()
    {
        using var database = new TestDatabase();
        var order = await new CartService(database.Context)
            .CreateOrderFromCartAsync(123, "Via Roma 1", "3331234567");

        Assert.Null(order);
        Assert.Empty(await database.Context.Orders.ToListAsync());
    }

    [Fact]
    public async Task Checkout_WhenOrderCannotBeSaved_RollsBackStockAndKeepsCart()
    {
        using var database = new TestDatabase();
        await VerifySaveFailureAsync(database.Context);
    }

    internal static async Task<ApplicationUser> SeedCartAsync(ApplicationDbContext context)
    {
        var user = new ApplicationUser { UserName = "checkout-user" };
        context.Carts.Add(new Cart
        {
            User = user,
            Items =
            [
                new CartItem { FoodItemId = 1, Quantity = 2 },
                new CartItem { FoodItemId = 2, Quantity = 1 }
            ]
        });
        foreach (var food in await context.FoodItems.ToListAsync())
            food.StockQuantity = 5;
        await context.SaveChangesAsync();
        return user;
    }

    internal static async Task VerifyStockFailureAsync(ApplicationDbContext context, int stock, bool available)
    {
        var user = await SeedCartAsync(context);
        var food = await context.FoodItems.FindAsync(2);
        food!.StockQuantity = stock;
        food.IsAvailable = available;
        await context.SaveChangesAsync();

        var order = await new CartService(context)
            .CreateOrderFromCartAsync(user.Id, "Via Roma 1", "3331234567");

        Assert.Null(order);
        // A later SaveChanges on the same context must not persist a rolled-back decrement.
        await context.SaveChangesAsync();
        await AssertUnchangedAsync(context, stock);
    }

    internal static async Task VerifySaveFailureAsync(ApplicationDbContext context)
    {
        var user = await SeedCartAsync(context);
        // A missing required address makes the INSERT fail after both stock updates.
        await Assert.ThrowsAsync<DbUpdateException>(() => new CartService(context)
            .CreateOrderFromCartAsync(user.Id, null!, "3331234567"));

        await AssertUnchangedAsync(context, 5);
    }

    private static async Task AssertUnchangedAsync(ApplicationDbContext context, int secondStock)
    {
        // Read persisted values rather than stale tracked FoodItems after ExecuteUpdateAsync.
        context.ChangeTracker.Clear();
        Assert.Equal(5, (await context.FoodItems.FindAsync(1))!.StockQuantity);
        Assert.Equal(secondStock, (await context.FoodItems.FindAsync(2))!.StockQuantity);
        Assert.Equal(2, await context.CartItems.CountAsync());
        Assert.Empty(await context.Orders.ToListAsync());
        Assert.Empty(await context.OrderItems.ToListAsync());
    }
}
