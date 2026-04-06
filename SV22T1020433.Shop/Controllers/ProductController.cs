using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Catalog;
using SV22T1020433.Models.Common;

namespace SV22T1020433.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;

        public async Task<IActionResult> Index(int categoryID = 0, string searchValue = "", int page = 1, decimal minPrice = 0, decimal maxPrice = 0)
        {
            var input = new ProductSearchInput()
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue ?? "",
                CategoryID = categoryID,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            var result = await CatalogDataService.ListProductsAsync(input);
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { PageSize = 0 });
            ViewBag.CurrentInput = input;

            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            
            // Lấy thêm sản phẩm cùng loại
            var relatedInput = new ProductSearchInput { 
                Page = 1, 
                PageSize = 4, 
                CategoryID = product.CategoryID ?? 0 
            };
            ViewBag.RelatedProducts = (await CatalogDataService.ListProductsAsync(relatedInput)).DataItems;

            return View(product);
        }
    }
}
