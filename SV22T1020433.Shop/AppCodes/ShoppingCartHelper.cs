using SV22T1020433.Models.Sales;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SV22T1020433.Shop
{
    public static class ShoppingCartHelper
    {
        private const string CART_KEY = "ShoppingCart";

        public static List<OrderDetailViewInfo> GetCart(HttpContext context)
        {
            var cartJson = context.Session.GetString(CART_KEY);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<OrderDetailViewInfo>();
            }
            return JsonSerializer.Deserialize<List<OrderDetailViewInfo>>(cartJson) ?? new List<OrderDetailViewInfo>();
        }

        public static void SaveCart(HttpContext context, List<OrderDetailViewInfo> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            context.Session.SetString(CART_KEY, cartJson);
        }

        public static void AddToCart(HttpContext context, OrderDetailViewInfo item)
        {
            var cart = GetCart(context);
            var existItem = cart.FirstOrDefault(m => m.ProductID == item.ProductID);
            if (existItem == null)
            {
                cart.Add(item);
            }
            else
            {
                existItem.Quantity += item.Quantity;
                existItem.SalePrice = item.SalePrice;
            }
            SaveCart(context, cart);
        }

        public static void UpdateQuantity(HttpContext context, int productID, int quantity)
        {
            var cart = GetCart(context);
            var item = cart.FirstOrDefault(m => m.ProductID == productID);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(context, cart);
            }
        }

        public static void RemoveFromCart(HttpContext context, int productID)
        {
            var cart = GetCart(context);
            var item = cart.FirstOrDefault(m => m.ProductID == productID);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(context, cart);
            }
        }

        public static void ClearCart(HttpContext context)
        {
            context.Session.Remove(CART_KEY);
        }

        public static int GetCartCount(HttpContext context)
        {
            var cart = GetCart(context);
            return cart.Sum(m => m.Quantity);
        }
    }
}
