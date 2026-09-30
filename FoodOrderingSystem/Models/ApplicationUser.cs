using Microsoft.AspNetCore.Identity;

namespace FoodOrderingSystem.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }
        
        public Cart Cart { get; set; }
    }
}
