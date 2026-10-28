using System.Data.Common;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodOrderingSystem.Tests;

// Opt in with a SQLSERVER_TEST_CONNECTION_STRING whose login may create test databases.
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION_STRING")))
            Skip = "Set SQLSERVER_TEST_CONNECTION_STRING to run isolated SQL Server integration tests.";
    }
}

public class SqlServerCheckoutTests
{
    [SqlServerFact]
    public async Task Checkout_WhenTwoUsersReadTheLastItem_OnlyOneOrderSucceeds()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        int firstUserId;
        int secondUserId;
        await using (var setup = database.CreateContext())
        {
            var firstUser = new ApplicationUser { UserName = "first-buyer" };
            var secondUser = new ApplicationUser { UserName = "second-buyer" };
            setup.Carts.AddRange(
                new Cart { User = firstUser, Items = [new CartItem { FoodItemId = 1, Quantity = 1 }] },
                new Cart { User = secondUser, Items = [new CartItem { FoodItemId = 1, Quantity = 1 }] });
            (await setup.FoodItems.FindAsync(1))!.StockQuantity = 1;
            await setup.SaveChangesAsync();
            firstUserId = firstUser.Id;
            secondUserId = secondUser.Id;
        }

        // Both checkouts must finish loading stock=1 before either can execute its UPDATE.
        var barrier = new StockUpdateBarrier();
        await using var first = database.CreateContext(barrier);
        await using var second = database.CreateContext(barrier);
        var results = await Task.WhenAll(
            new CartService(first).CreateOrderFromCartAsync(firstUserId, "Via Roma 1", "3331234567"),
            new CartService(second).CreateOrderFromCartAsync(secondUserId, "Via Roma 2", "3331234568"));

        Assert.Equal(2, barrier.Arrivals);
        var winner = Assert.Single(results, o => o is not null)!;
        Assert.Single(results, o => o is null);
        await using var verify = database.CreateContext();
        Assert.Equal(0, (await verify.FoodItems.FindAsync(1))!.StockQuantity);
        Assert.Equal(1, await verify.Orders.CountAsync());
        Assert.Equal(1, await verify.OrderItems.CountAsync());
        var remainingItem = await verify.CartItems.Include(i => i.Cart).SingleAsync();
        Assert.NotEqual(winner.UserId, remainingItem.Cart.UserId);
        Assert.Equal(1, remainingItem.Quantity);
    }

    [SqlServerFact]
    public async Task Checkout_WhenLaterProductFails_RollsBackEarlierDecrement()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await CheckoutTests.VerifyStockFailureAsync(context, 0, true);
    }

    [SqlServerFact]
    public async Task Checkout_WhenSavingOrderFails_RollsBackAllStockUpdates()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await CheckoutTests.VerifySaveFailureAsync(context);
    }

    private sealed class StockUpdateBarrier : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public int Arrivals => _arrivals;

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                if (Interlocked.Increment(ref _arrivals) == 2)
                    _ready.TrySetResult();
                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }

    private sealed class SqlServerTestDatabase : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly string _databaseName = $"FoodOrderingCheckoutTests_{Guid.NewGuid():N}";

        private SqlServerTestDatabase()
        {
            var builder = new SqlConnectionStringBuilder(
                Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION_STRING"));
            builder.InitialCatalog = _databaseName;
            builder.Pooling = false;
            _connectionString = builder.ConnectionString;
        }

        public ApplicationDbContext CreateContext(params IInterceptor[] interceptors) => new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(_connectionString)
                .AddInterceptors(interceptors)
                .Options);

        public static async Task<SqlServerTestDatabase> CreateAsync()
        {
            var database = new SqlServerTestDatabase();
            try
            {
                await using var context = database.CreateContext();
                await context.Database.EnsureCreatedAsync();
                return database;
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Only the unique database generated by this fixture may be removed.
            if (new SqlConnectionStringBuilder(_connectionString).InitialCatalog != _databaseName ||
                !_databaseName.StartsWith("FoodOrderingCheckoutTests_", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to delete a non-test database.");

            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }
    }
}
