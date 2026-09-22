using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Order? GetById(int orderId)
    {
        return _context.Orders.Find(orderId);
    }

    public OperationResult ConfirmOrder(int orderId)
    {
        var order = _context.Orders.Find(orderId);

        if (order == null)
            return OperationResult.NotFound("Order not found.");

        if (order.Status != "Pending")
            return OperationResult.InvalidStatus(
                "Only pending orders can be confirmed.");

        order.Status = "Confirmed";
        _context.SaveChanges();

        return OperationResult.Success();
    }

    public OperationResult AdvanceStatus(int orderId)
    {
        var order = _context.Orders.Find(orderId);

        if (order == null)
            return OperationResult.NotFound("Order not found.");

        string? nextStatus = order.Status switch
        {
            "Confirmed" => "Preparing",
            "Preparing" => "OutForDelivery",
            "OutForDelivery" => "Delivered",
            _ => null
        };

        if (nextStatus is null)
        {
            return OperationResult.InvalidStatus(
                $"Order cannot advance from status {order.Status}.");
        }

        order.Status = nextStatus;
        _context.SaveChanges();

        return OperationResult.Success();
    }

    public OperationResult CancelOrder(int orderId)
    {
        var order = _context.Orders.Find(orderId);

        if (order is null)
            return OperationResult.NotFound("Order not found.");

        if (order.Status is "Delivered" or "Cancelled")
            return OperationResult.InvalidStatus(
                $"An order with status {order.Status} cannot be cancelled.");

        order.Status = "Cancelled";
        _context.SaveChanges();

        return OperationResult.Success();
    }
}
