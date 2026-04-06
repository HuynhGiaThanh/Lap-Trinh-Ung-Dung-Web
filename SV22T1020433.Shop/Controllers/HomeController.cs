using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Catalog;

namespace SV22T1020433.Shop.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            // Lấy 8 sản phẩm mới nhất để hiển thị ở trang chủ
            var input = new ProductSearchInput() { Page = 1, PageSize = 8, SearchValue = "" };
            var result = await CatalogDataService.ListProductsAsync(input);
            return View(result.DataItems);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
