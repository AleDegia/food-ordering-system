using FoodOrderingSystem.Controllers;
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.ViewModels;
using FoodOrderingSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FoodOrderingSystem.Tests;

public class LoginGuestCartTests
{
    [Fact]
    public async Task Login_TransfersGuestCartToAuthenticatedUser()
    {
        using var database = new TestDatabase();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();
        services.AddMvcCore();
        services.AddDataProtection();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddSingleton(database.Context);
        services.AddHttpContextAccessor();
        services.AddIdentity<ApplicationUser, IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider, Session = new TestSession() };
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "cliente", Email = "cliente@example.com" };
        const string password = "Password1!";
        Assert.True((await users.CreateAsync(user, password)).Succeeded);

        var food = database.Context.FoodItems.Find(1)!;
        food.StockQuantity = 10;
        database.Context.SaveChanges();
        var existingCart = new Cart { UserId = user.Id };
        database.Context.Carts.Add(existingCart);
        database.Context.SaveChanges();
        database.Context.CartItems.Add(new CartItem { CartId = existingCart.Id, FoodItemId = food.Id, Quantity = 1 });
        database.Context.SaveChanges();

        var guestCart = new SessionCartService(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>(), database.Context);
        Assert.True(guestCart.AddItem(food.Id, 2).IsSuccess);
        Assert.False(context.User.Identity!.IsAuthenticated);
        var controller = new AccountController(users, signIn, database.Context, guestCart)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = context,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
            }
        };

        var result = await controller.Login(new LoginViewModel { Username = user.UserName!, Password = password });

        Assert.IsType<RedirectToActionResult>(result);
        var item = Assert.Single(database.Context.CartItems.Where(i => i.CartId == existingCart.Id));
        Assert.Equal(3, item.Quantity);
        Assert.Empty(guestCart.GetCart());
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();
        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _values.Keys;
        public void Clear() => _values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public void Set(string key, byte[] value) => _values[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value!);
    }
}
