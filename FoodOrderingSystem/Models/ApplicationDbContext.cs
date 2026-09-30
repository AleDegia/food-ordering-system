using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FoodOrderingSystem.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<FoodItem> FoodItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // per chiamare la logica di configurazione in predefinita di IdentityDbContext del suo OnModelCreating
            base.OnModelCreating(modelBuilder);
            // Seed initial data
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Pizza", Description = "Delicious pizzas" },
                new Category { Id = 2, Name = "Burgers", Description = "Juicy burgers" },
                new Category { Id = 3, Name = "Drinks", Description = "Refreshing beverages" }
            );

            modelBuilder.Entity<FoodItem>().HasData(
                new FoodItem { Id = 1, Name = "Margherita Pizza", Description = "Classic tomato and cheese", Price = 12.99m, CategoryId = 1, ImageUrl = "/images/Margheritapizza.jpg" },
                new FoodItem { Id = 2, Name = "Pepperoni Pizza", Description = "Spicy pepperoni with cheese", Price = 14.99m, CategoryId = 1, ImageUrl = "/images/Pepperonipizza.jpg" },
                new FoodItem { Id = 3, Name = "Cheeseburger", Description = "Beef patty with cheese", Price = 9.99m, CategoryId = 2, ImageUrl = "/images/Cheeseburger.jpg" }
            );

             modelBuilder.Entity<Cart>()                // configura l'entità Cart
                .HasOne(c => c.User)                    // ogni Cart ha un solo User
                .WithOne(u => u.Cart)                   // ogni User ha al massimo un solo Cart
                .HasForeignKey<Cart>(c => c.UserId);    // la FK è Cart.UserId
        }
    }
}