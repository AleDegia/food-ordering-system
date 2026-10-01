using FoodOrderingSystem.Models;
using FoodOrderingSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>()
  .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
});

// Service layer
builder.Services.AddScoped<IOrderService, OrderService>();  //quando qualcuno chiede IOrderService va a creare OrderService
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SessionCartService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();        // continua a servire per il carrello
app.UseAuthentication(); // legge il cookie Identity
app.UseAuthorization();  // applica [Authorize]


// Serve the images uploaded by administrators after the application has started.
app.UseStaticFiles();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    var roleManager = scope.ServiceProvider
         .GetRequiredService<RoleManager<IdentityRole<int>>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole<int>("Admin"));
    }

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var admin = await userManager.FindByEmailAsync("miaMail@example.com");

    if (admin != null && !await userManager.IsInRoleAsync(admin, "Admin"))
    {
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}

app.Run();
