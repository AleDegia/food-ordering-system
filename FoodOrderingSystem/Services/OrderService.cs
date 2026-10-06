using FoodOrderingSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace FoodOrderingSystem.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static readonly HashSet<string>
     ValidOrderStatuses = new()
     {
          "Pending",
          "Confirmed",
          "Preparing",
          "OutForDelivery",
          "Delivered",
          "Cancelled"
     };

    public Order? GetOrdersById(int orderId)
    {
        return _context.Orders.Find(orderId);
    }

    public List<Order> GetMyOrders (string status, string sortOrder, DateTime? fromDate, DateTime? toDate, int userId)
    {
        var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .Where(o => o.UserId == userId)
                .AsQueryable();

        // Filter by Status
        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            orders = orders.Where(o => o.Status == status);
        }

        // Filter by Date Range
        if (fromDate.HasValue)
        {
            orders = orders.Where(o => o.OrderDate >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            // Add 1 day to include the entire 'To' date
            orders = orders.Where(o => o.OrderDate <= toDate.Value.AddDays(1));
        }

        // Sorting Logic (mando a frontend la lista degli ordini gia ordinata)
        orders = sortOrder switch
        {
            "date_asc" => orders.OrderBy(o => o.OrderDate),
            "total_desc" => orders.OrderByDescending(o => o.TotalAmount),
            "total_asc" => orders.OrderBy(o => o.TotalAmount),
            _ => orders.OrderByDescending(o => o.OrderDate) // _ è il default dello switch compatto: newest first
        };
        return orders.ToList();
    }

    public List<Order> GetFilteredOrders(string status, string searchString, DateTime? fromDate, DateTime? toDate, string sortOrder)
    {
        var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .AsQueryable();

        // Filtro stato
        if (!string.IsNullOrEmpty(status) &&
            status != "All" &&
            ValidOrderStatuses.Contains(status))
        {
            orders = orders.Where(o => o.Status == status);
        }

        // Ricerca cliente
        if (!string.IsNullOrEmpty(searchString))
        {
            orders = orders.Where(o => o.User.FullName == searchString
                               || o.User.UserName.Contains(searchString));
        }

        // Intervallo date
        if (fromDate.HasValue)
        {
            orders = orders.Where(o => o.OrderDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            orders = orders.Where(o =>
                o.OrderDate < toDate.Value.Date.AddDays(1));
        }

        // Ordinamento
        orders = sortOrder switch
        {
            "date_asc" => orders.OrderBy(o => o.OrderDate),
            "date_desc" => orders.OrderByDescending(o => o.OrderDate),
            "total_asc" => orders.OrderBy(o => o.TotalAmount),
            "total_desc" => orders.OrderByDescending(o => o.TotalAmount),
            "status" => orders.OrderBy(o => o.Status),
            "status_desc" => orders.OrderByDescending(o => o.Status),
            _ => orders.OrderByDescending(o => o.OrderDate)
        };
        return orders.ToList();
    }

    public Order GetOrderDetails(int id)
    {
        var order = _context.Orders
                .Include(o => o.User)                                       //uso navigation property per includere i dettagli dell'utente associato all'ordine (x ogni ordine voglio vedere anche i dettagli dell'utente che l'ha fatto)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.FoodItem)
                .FirstOrDefault(o => o.Id == id);                           //Esegue

        return order;
    }

    public Order GetUserOrderDetails(int orderId, int userId)
    {
        var order = _context.Orders
               .Include(o => o.OrderItems)
               .ThenInclude(oi => oi.FoodItem)
               .FirstOrDefault(o => o.Id == orderId && o.UserId == userId);

        return order;
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

    public OperationResult UpdateOrderStatus(int orderId, string newStatus)
    {
        if (!ValidOrderStatuses.Contains(newStatus))
            return
            OperationResult.InvalidStatus("Invalid order status.");
  

        var order = _context.Orders.Find(orderId);

        if (order is null)
            return OperationResult.NotFound("Order not found");

        if (order.Status == newStatus)
            return OperationResult.Success();

        // Annullamento ammesso finché non è consegnato / già annullato
        if (newStatus == "Cancelled")
            return CancelOrder(orderId);

        // Pending può diventare solo Confirmed
        if (order.Status == "Pending")
        {
            if (newStatus != "Confirmed")
                return OperationResult.InvalidStatus(
                    "A pending order can only be confirmed or cancelled.");
  
          return ConfirmOrder(orderId);
        }

        // Per gli stati successivi deve procedere di uno alla volta
        var expectedNextStatus = order.Status switch
        {
            "Confirmed" => "Preparing",
            "Preparing" => "OutForDelivery",
            "OutForDelivery" => "Delivered",
            _ => null
        };

        if (expectedNextStatus != newStatus)
            return OperationResult.InvalidStatus(
                $"Order cannot change from{ order.Status}to { newStatus}.");

      order.Status = newStatus;
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

    public OperationResult CancelUserOrder(int orderId, int userId)
    {
        var order = _context.Orders
            .FirstOrDefault(o => o.Id == orderId && o.UserId == userId);

        if (order is null)
            return OperationResult.NotFound("Order not found.");

        if (order.Status != "Pending" && order.Status != "Confirmed")
        {
            return OperationResult.InvalidStatus(
                "Order cannot be cancelled at this stage.");
        }

        order.Status = "Cancelled";
        _context.SaveChanges();

        return OperationResult.Success();
    }
}
