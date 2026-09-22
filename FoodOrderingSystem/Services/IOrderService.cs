using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Services
{
    public interface IOrderService
    {
        Order? GetById(int orderId);
        OperationResult ConfirmOrder(int orderId);
        OperationResult AdvanceStatus(int orderId);
        OperationResult CancelOrder(int orderId);
    }
}
