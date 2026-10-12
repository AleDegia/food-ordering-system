using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;

namespace FoodOrderingSystem.Tests;

public class OrderServiceTests
{
    [Fact]
    public void ConfirmOrder_WhenOrderIsPending_ChangesStatusToConfirmed()
    {
        using var database = new TestDatabase();
        var user = CreateUser();
        var order = CreateOrder(user, "Pending");
        database.Context.AddRange(user, order);
        database.Context.SaveChanges();

        var service = new OrderService(database.Context);

        var result = service.ConfirmOrder(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(OperationResultCode.Success, result.Code);
        Assert.Equal("Confirmed", database.Context.Orders.Find(order.Id)!.Status);
    }

    [Fact]
    public void UpdateOrderStatus_WhenItSkipsAStatusReturnsInvalidStatus()
    {
        using var database = new TestDatabase();
        var user = CreateUser();
        var order = CreateOrder(user, "Confirmed");
        database.Context.AddRange(user, order);
        database.Context.SaveChanges();

        var service = new OrderService(database.Context);

        var result = service.UpdateOrderStatus(order.Id, "Delivered");

        Assert.Equal(OperationResultCode.InvalidStatus, result.Code);
        Assert.Equal("Confirmed", database.Context.Orders.Find(order.Id)!.Status);
    }

    private static ApplicationUser CreateUser() => new()
    {
        Id = 1,
        UserName = "utente-a",
        Email = "a@test.it"
    };

    private static Order CreateOrder(ApplicationUser user, string status) => new()
    {
        UserId = user.Id,
        DeliveryAddress = "Via Roma 1",
        PhoneNumber = "3331234567",
        Status = status,
        TotalAmount = 12.99m,
        OrderItems = new List<OrderItem>()
    };
}
