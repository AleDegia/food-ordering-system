using FoodOrderingSystem.Controllers;

namespace FoodOrderingSystem.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int UserId { get; set; }                                     //FK
        public ApplicationUser User { get; set; } = null!;                  //NP

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
