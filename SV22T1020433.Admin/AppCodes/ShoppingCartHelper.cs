using SV22T1020433.Models.Sales;
using System.Collections.Generic;

namespace SV22T1020433.Admin
{
    /// <summary>
    /// Lớp cung cấp các chức năng xử lý trên giỏ hàng (lưu trong session)
    /// </summary>
    public static class ShoppingCartHelper
    {
        private const string CART = "ShoppingCart";

        /// <summary>
        /// Lấy danh sách các mặt hàng đang có trong giỏ hàng
        /// </summary>
        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CART);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CART, cart);
            }
            return cart;
        }

        /// <summary>
        /// Tìm một mặt hàng cụ thể trong giỏ hàng dựa trên ProductID
        /// </summary>
        public static OrderDetailViewInfo? GetCartItem(int productID)
        {
            return GetShoppingCart().Find(m => m.ProductID == productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào giỏ hàng. 
        /// Nếu mặt hàng đã tồn tại thì tăng số lượng và cập nhật giá bán mới.
        /// </summary>
        public static void AddItemToCart(OrderDetailViewInfo item)
        {
            var cart = GetShoppingCart();
            var existItem = cart.Find(m => m.ProductID == item.ProductID);
            if (existItem == null)
            {
                cart.Add(item);
            }
            else
            {
                existItem.Quantity += item.Quantity;
                existItem.SalePrice = item.SalePrice;
            }
            ApplicationContext.SetSessionData(CART, cart);
        }

        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong giỏ hàng
        /// </summary>
        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            if (item != null)
            {
                item.Quantity = quantity;
                item.SalePrice = salePrice;
                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi giỏ hàng
        /// </summary>
        public static void RemoveItemFromCart(int productID)
        {
            var cart = GetShoppingCart();
            int index = cart.FindIndex(m => m.ProductID == productID);
            if (index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        public static void ClearCart()
        {
            ApplicationContext.SetSessionData(CART, new List<OrderDetailViewInfo>());
        }
    }
}
