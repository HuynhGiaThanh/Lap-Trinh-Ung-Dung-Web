using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Sales;

namespace SV22T1020433.Shop.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            var cart = ShoppingCartHelper.GetCart(HttpContext);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product != null)
            {
                var item = new OrderDetailViewInfo()
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Photo = product.Photo ?? "noproduct.png",
                    Unit = product.Unit,
                    Quantity = quantity,
                    SalePrice = product.Price
                };
                ShoppingCartHelper.AddToCart(HttpContext, item);
            }
            return RedirectToAction("Index");
        }

        public IActionResult Update(int id, int quantity)
        {
            if (quantity > 0)
            {
                ShoppingCartHelper.UpdateQuantity(HttpContext, id, quantity);
            }
            else
            {
                ShoppingCartHelper.RemoveFromCart(HttpContext, id);
            }
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            ShoppingCartHelper.RemoveFromCart(HttpContext, id);
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            ShoppingCartHelper.ClearCart(HttpContext);
            return RedirectToAction("Index");
        }
    }
}
