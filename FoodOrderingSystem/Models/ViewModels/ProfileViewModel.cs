using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string FullName { get; set; }

        public string Address { get; set; }

        public string Phone { get; set; }
    }
}