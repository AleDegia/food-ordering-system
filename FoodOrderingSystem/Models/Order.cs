using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }                 //FK (NavigationProperty + Id, per convenzione)
        public User User { get; set; }                  //Navigation Property

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public string DeliveryAddress { get; set; }

        public string PhoneNumber { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        public ICollection<OrderItem> OrderItems { get; set; }              //NP con relationship 1 a molti con OrderItem
    }
}