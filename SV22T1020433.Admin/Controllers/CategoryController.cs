using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Catalog;

namespace SV22T1020433.Admin.Controllers
{
    [Authorize]
    public class CategoryController : _BaseController
    {
        private const string CATEGORY_SEARCH_INPUT = "CategorySearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH_INPUT);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            return View(input);
        }

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            try
            {
                var result = await CatalogDataService.ListCategoriesAsync(input);
                ApplicationContext.SetSessionData(CATEGORY_SEARCH_INPUT, input);
                return View(result);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new PagedResult<Category>());
            }
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            var model = new Category()
            {
                CategoryID = 0
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật thông tin loại hàng";
                var model = await CatalogDataService.GetCategoryAsync(id);
                if (model == null)
                    return RedirectToAction("Index");
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật thông tin loại hàng";
            try
            {
                if (string.IsNullOrWhiteSpace(data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên loại hàng");

                if (string.IsNullOrEmpty(data.Description))
                    data.Description = "";

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (data.CategoryID == 0)
                    await CatalogDataService.AddCategoryAsync(data);
                else
                    await CatalogDataService.UpdateCategoryAsync(data);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", $"Có lỗi xảy ra: {ex.Message}");
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await CatalogDataService.DeleteCategoryAsync(id);
                    return RedirectToAction("Index");
                }

                var model = await CatalogDataService.GetCategoryAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                // Kiểm tra xem loại hàng này có đang được sử dụng ở các bảng khác không (VD: bảng Product)
                ViewBag.AllowDelete = !(await CatalogDataService.IsUsedCategoryAsync(id));
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
