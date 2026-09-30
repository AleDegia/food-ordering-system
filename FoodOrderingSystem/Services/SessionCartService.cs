using FoodOrderingSystem.Controllers;
using FoodOrderingSystem.Models;
using Newtonsoft.Json;

namespace FoodOrderingSystem.Services
{
    public class SessionCartService
    {
        private readonly IHttpContextAccessor
        _httpContextAccessor;

        public SessionCartService(IHttpContextAccessor
        httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public List<SessionCartItem> GetCart()
        {
            var session = _httpContextAccessor.HttpContext!.Session;
            var cartJson = session.GetString("Cart");

            return string.IsNullOrEmpty(cartJson)
              ? new List<SessionCartItem>()
              :
              JsonConvert.DeserializeObject<List<SessionCartItem>>(cartJson)
                ?? new List<SessionCartItem>();
        }
    }
}
