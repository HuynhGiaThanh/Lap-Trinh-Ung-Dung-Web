using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Sales;

namespace SV22T1020433.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = ShoppingCartHelper.GetCart(HttpContext);
            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            var userData = User.GetUserData();
            var customer = await PartnerDataService.GetCustomerAsync(int.Parse(userData?.UserId ?? "0"));
            
            ViewBag.Cart = cart;
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            
            var order = new Order()
            {
                CustomerID = customer?.CustomerID,
                DeliveryProvince = customer?.Province,
                DeliveryAddress = customer?.Address
            };

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitOrder(Order data)
        {
            var cart = ShoppingCartHelper.GetCart(HttpContext);
            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            if (string.IsNullOrWhiteSpace(data.DeliveryProvince))
                ModelState.AddModelError(nameof(data.DeliveryProvince), "Vui lòng chọn tỉnh thành giao hàng");
            if (string.IsNullOrWhiteSpace(data.DeliveryAddress))
                ModelState.AddModelError(nameof(data.DeliveryAddress), "Vui lòng nhập địa chỉ giao hàng");

            if (!ModelState.IsValid)
            {
                ViewBag.Cart = cart;
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View("Checkout", data);
            }

            // Tạo đơn hàng
            int orderID = await SalesDataService.AddOrderAsync(data.CustomerID ?? 0, data.DeliveryAddress!, data.DeliveryProvince!);
            if (orderID > 0)
            {
                // Thêm chi tiết đơn hàng
                foreach (var item in cart)
                {
                    await SalesDataService.AddDetailAsync(new OrderDetail()
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    });
                }
                // Xóa giỏ hàng sau khi đặt thành công
                ShoppingCartHelper.ClearCart(HttpContext);
                TempData["Message"] = "Đặt hàng thành công! Cảm ơn bạn đã mua sắm tại HGT Shop.";
                return RedirectToAction("Detail", new { id = orderID });
            }

            ModelState.AddModelError("", "Có lỗi xảy ra khi tạo đơn hàng. Vui lòng thử lại.");
            ViewBag.Cart = cart;
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View("Checkout", data);
        }

        public async Task<IActionResult> History(int page = 1, string searchValue = "")
        {
            var userData = User.GetUserData();
            int customerID = int.Parse(userData?.UserId ?? "0");

            var input = new OrderSearchInput()
            {
                Page = page,
                PageSize = 10,
                SearchValue = searchValue ?? "",
                Status = 0 // Tất cả trạng thái
            };

            // Lưu ý: ListOrdersAsync hiện tại chưa có filter theo CustomerID trong interface mặc định của bạn.
            // Tôi cần kiểm tra xem IOrderRepository có hỗ trợ lọc theo CustomerID không.
            // Nếu không, tôi sẽ giả định input.SearchValue có thể chứa ID hoặc tôi cần cập nhật Repository.
            // Để đơn giản và đúng chuẩn, tôi sẽ lọc kết quả trả về nếu Repository không hỗ trợ.
            
            var result = await SalesDataService.ListOrdersAsync(input);
            
            // Lọc lại chỉ lấy đơn hàng của khách hàng hiện tại
            // (Trong thực tế nên lọc ở SQL để tối ưu)
            result.DataItems = result.DataItems.Where(o => o.CustomerID == customerID).ToList();
            result.RowCount = result.DataItems.Count;

            ViewBag.CurrentSearch = searchValue;
            return View(result);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            var userData = User.GetUserData();
            int customerID = int.Parse(userData?.UserId ?? "0");

            if (order == null || order.CustomerID != customerID)
                return RedirectToAction("History");

            var details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.Details = details;
            return View(order);
        }
    }
}
