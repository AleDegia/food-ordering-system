using FoodOrderingSystem.Models;
using System.Net.NetworkInformation;

namespace FoodOrderingSystem.Services
{
    public interface IOrderService
    {
        Order? GetOrdersById(int orderId);
        OperationResult ConfirmOrder(int orderId);
        OperationResult AdvanceStatus(int orderId);
        OperationResult UpdateOrderStatus(int orderId, string status);
        OperationResult CancelOrder(int orderId);
        OperationResult CancelUserOrder(int orderId, int userId);
        List<Order> GetFilteredOrders(string status, string searchString, DateTime? fromDate, DateTime? toDate, string sortOrder);
        Order GetOrderDetails(int id);
        Order GetUserOrderDetails(int orderId, int userId);
        List<Order> GetMyOrders(string status, string sortOrder, DateTime? fromDate, DateTime? toDate, int userId);
        //List<Order> GetOrdersByUserId(int userId);
    }
}
