using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Catalog;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Sales;

namespace SV22T1020433.Admin.Controllers
{
    [Authorize]
    public class OrderController : _BaseController
    {
        private const string ORDER_SEARCH_INPUT = "OrderSearchInput";
        private const string SEARCH_PRODUCT_INPUT = "SearchProductInputForOrder";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH_INPUT);
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    Status = 0,
                    DateRange = ""
                };
            }
            return View(input);
        }

        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            try
            {
                var result = await SalesDataService.ListOrdersAsync(input);
                ApplicationContext.SetSessionData(ORDER_SEARCH_INPUT, input);
                return View(result);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new PagedResult<OrderViewInfo>());
            }
        }

        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null)
                    return RedirectToAction("Index");

                var details = await SalesDataService.ListDetailsAsync(id);
                ViewBag.Order = order;
                ViewBag.Details = details;
                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT_INPUT);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 5,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0
                };
            }
            return View(input);
        }

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            try
            {
                var result = await CatalogDataService.ListProductsAsync(input);
                ApplicationContext.SetSessionData(SEARCH_PRODUCT_INPUT, input);
                return View(result);
            }
            catch (Exception ex)
            {
                return Json(ApiResult.Fail(ex.Message));
            }
        }

        public IActionResult ShowCart()
        {
            return View(ShoppingCartHelper.GetShoppingCart());
        }

        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productId = 0, int quantity = 0, decimal salePrice = 0)
        {
            try
            {
                if (productId <= 0) return Json(ApiResult.Fail("Mã mặt hàng không hợp lệ"));
                if (quantity <= 0) return Json(ApiResult.Fail("Số lượng phải lớn hơn 0"));
                if (salePrice < 0) return Json(ApiResult.Fail("Giá bán không hợp lệ"));

                var product = await CatalogDataService.GetProductAsync(productId);
                if (product == null) return Json(ApiResult.Fail("Mặt hàng không tồn tại"));
                if (!product.IsSelling) return Json(ApiResult.Fail("Mặt hàng hiện không được bán"));

                var item = new OrderDetailViewInfo()
                {
                    ProductID = productId,
                    ProductName = product.ProductName,
                    Photo = product.Photo ?? "noproduct.png",
                    Unit = product.Unit,
                    Quantity = quantity,
                    SalePrice = salePrice
                };
                ShoppingCartHelper.AddItemToCart(item);
                return Json(ApiResult.Success());
            }
            catch (Exception ex)
            {
                return Json(ApiResult.Fail(ex.Message));
            }
        }

        [HttpPost]
        public IActionResult RemoveCartItem(int id)
        {
            ShoppingCartHelper.RemoveItemFromCart(id);
            return Json(ApiResult.Success());
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            ShoppingCartHelper.ClearCart();
            return Json(ApiResult.Success());
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string deliveryProvince = "", string deliveryAddress = "")
        {
            try
            {
                var cart = ShoppingCartHelper.GetShoppingCart();
                if (cart == null || cart.Count == 0) return Json(ApiResult.Fail("Giỏ hàng đang trống"));
                if (customerID <= 0) return Json(ApiResult.Fail("Vui lòng chọn khách hàng"));
                if (string.IsNullOrWhiteSpace(deliveryProvince)) return Json(ApiResult.Fail("Vui lòng chọn tỉnh/thành giao hàng"));
                if (string.IsNullOrWhiteSpace(deliveryAddress)) return Json(ApiResult.Fail("Vui lòng nhập địa chỉ giao hàng"));

                int employeeID = int.Parse(User.GetUserData()?.UserId ?? "0");

                int orderID = await SalesDataService.AddOrderAsync(customerID, deliveryAddress, deliveryProvince);
                if (orderID > 0)
                {
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
                    ShoppingCartHelper.ClearCart();
                    return Json(ApiResult.Success(orderID.ToString()));
                }
                return Json(ApiResult.Fail("Không tạo được đơn hàng"));
            }
            catch (Exception ex)
            {
                return Json(ApiResult.Fail(ex.Message));
            }
        }

        public async Task<IActionResult> Accept(int id)
        {
            try
            {
                int employeeID = int.Parse(User.GetUserData()?.UserId ?? "0");
                bool ok = await SalesDataService.AcceptOrderAsync(id, employeeID);
                if (!ok) TempData["Error"] = "Không thể chấp nhận đơn hàng này";
                return RedirectToAction("Detail", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }

        public async Task<IActionResult> Shipping(int id, int shipperID = 0)
        {
            try
            {
                if (shipperID <= 0)
                {
                    ViewBag.OrderID = id;
                    ViewBag.Shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = "" });
                    return View();
                }
                bool ok = await SalesDataService.ShipOrderAsync(id, shipperID);
                if (!ok) TempData["Error"] = "Không thể chuyển đơn hàng sang trạng thái đang giao hàng";
                return RedirectToAction("Detail", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }

        public async Task<IActionResult> Finish(int id)
        {
            try
            {
                bool ok = await SalesDataService.CompleteOrderAsync(id);
                if (!ok) TempData["Error"] = "Không thể xác nhận hoàn tất đơn hàng này";
                return RedirectToAction("Detail", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }

        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                bool ok = await SalesDataService.CancelOrderAsync(id);
                if (!ok) TempData["Error"] = "Không thể hủy đơn hàng này";
                return RedirectToAction("Detail", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }

        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                int employeeID = int.Parse(User.GetUserData()?.UserId ?? "0");
                bool ok = await SalesDataService.RejectOrderAsync(id, employeeID);
                if (!ok) TempData["Error"] = "Không thể từ chối đơn hàng này";
                return RedirectToAction("Detail", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    bool ok = await SalesDataService.DeleteOrderAsync(id);
                    if (!ok)
                    {
                        TempData["Error"] = "Không thể xóa đơn hàng này (đơn hàng phải ở trạng thái Hủy hoặc Bị từ chối)";
                        return RedirectToAction("Detail", new { id });
                    }
                    return RedirectToAction("Index");
                }
                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null) return RedirectToAction("Index");
                return View(order);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }
    }
}
