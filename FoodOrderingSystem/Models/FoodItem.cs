using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class FoodItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(0.01, 1000)]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }

        public int CategoryId { get; set; }                      // Foreign Key
        public Category Category { get; set; }                   // Navigation Property

        public bool IsAvailable { get; set; } = true;
    }
}