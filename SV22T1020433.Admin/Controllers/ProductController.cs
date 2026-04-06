using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Catalog;

namespace SV22T1020433.Admin.Controllers
{
    [Authorize]
    public class ProductController : _BaseController
    {
        private const string PRODUCT_SEARCH_INPUT = "ProductSearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH_INPUT);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }
            return View(input);
        }

        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            try
            {
                var result = await CatalogDataService.ListProductsAsync(input);
                ApplicationContext.SetSessionData(PRODUCT_SEARCH_INPUT, input);
                return View(result);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new PagedResult<Product>());
            }
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";
            var model = new Product()
            {
                ProductID = 0,
                IsSelling = true,
                Price = 0
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật thông tin mặt hàng";
                var model = await CatalogDataService.GetProductAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.ProductID = id;
                ViewBag.ProductAttributes = await CatalogDataService.ListAttributesAsync(id);
                ViewBag.ProductPhotos = await CatalogDataService.ListPhotosAsync(id);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật thông tin mặt hàng";
            try
            {
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Vui lòng nhập tên mặt hàng");
                if (data.CategoryID <= 0)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");
                if (data.SupplierID <= 0)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");
                if (string.IsNullOrWhiteSpace(data.Unit))
                    ModelState.AddModelError(nameof(data.Unit), "Vui lòng nhập đơn vị tính");
                if (data.Price < 0)
                    ModelState.AddModelError(nameof(data.Price), "Giá bán không hợp lệ");

                data.ProductDescription = data.ProductDescription ?? "";

                // Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                    string filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "products", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }
                else if (data.ProductID > 0 && string.IsNullOrWhiteSpace(data.Photo))
                {
                    var oldData = await CatalogDataService.GetProductAsync(data.ProductID);
                    data.Photo = oldData?.Photo;
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ProductID = data.ProductID;
                    ViewBag.ProductAttributes = data.ProductID > 0 ? await CatalogDataService.ListAttributesAsync(data.ProductID) : new List<ProductAttribute>();
                    ViewBag.ProductPhotos = data.ProductID > 0 ? await CatalogDataService.ListPhotosAsync(data.ProductID) : new List<ProductPhoto>();
                    return View("Edit", data);
                }

                if (data.ProductID == 0)
                    await CatalogDataService.AddProductAsync(data);
                else
                    await CatalogDataService.UpdateProductAsync(data);

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
                    await CatalogDataService.DeleteProductAsync(id);
                    return RedirectToAction("Index");
                }

                var model = await CatalogDataService.GetProductAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.AllowDelete = !(await CatalogDataService.IsUsedProductAsync(id));
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // --- QUẢN LÝ THUỘC TÍNH (ATTRIBUTES) ---

        public async Task<IActionResult> EditAttribute(int id, long attributeId)
        {
            try
            {
                if (attributeId == 0)
                {
                    ViewBag.Title = "Bổ sung thuộc tính";
                    var model = new ProductAttribute() { ProductID = id, AttributeID = 0 };
                    return View("EditAttribute", model);
                }
                else
                {
                    ViewBag.Title = "Cập nhật thuộc tính";
                    var model = await CatalogDataService.GetAttributeAsync(attributeId);
                    if (model == null || model.ProductID != id)
                        return RedirectToAction("Edit", new { id });
                    return View("EditAttribute", model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Edit", new { id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.AttributeName))
                    ModelState.AddModelError(nameof(data.AttributeName), "Vui lòng nhập tên thuộc tính");
                if (string.IsNullOrWhiteSpace(data.AttributeValue))
                    ModelState.AddModelError(nameof(data.AttributeValue), "Vui lòng nhập giá trị thuộc tính");
                if (data.DisplayOrder < 1)
                    ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị phải lớn hơn 0");

                if (!ModelState.IsValid)
                    return View("EditAttribute", data);

                if (data.AttributeID == 0)
                    await CatalogDataService.AddAttributeAsync(data);
                else
                    await CatalogDataService.UpdateAttributeAsync(data);

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", $"Có lỗi xảy ra: {ex.Message}");
                return View("EditAttribute", data);
            }
        }

        public async Task<IActionResult> DeleteAttribute(int id, long attributeId)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await CatalogDataService.DeleteAttributeAsync(attributeId);
                    return RedirectToAction("Edit", new { id });
                }

                var model = await CatalogDataService.GetAttributeAsync(attributeId);
                if (model == null || model.ProductID != id)
                    return RedirectToAction("Edit", new { id });

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Edit", new { id });
            }
        }

        // --- QUẢN LÝ ẢNH (PHOTOS) ---

        public async Task<IActionResult> EditPhoto(int id, long photoId)
        {
            try
            {
                if (photoId == 0)
                {
                    ViewBag.Title = "Bổ sung ảnh";
                    var model = new ProductPhoto() { ProductID = id, PhotoID = 0, DisplayOrder = 1, IsHidden = false };
                    return View("EditPhoto", model);
                }
                else
                {
                    ViewBag.Title = "Cập nhật ảnh";
                    var model = await CatalogDataService.GetPhotoAsync(photoId);
                    if (model == null || model.ProductID != id)
                        return RedirectToAction("Edit", new { id });
                    return View("EditPhoto", model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Edit", new { id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            try
            {
                data.Description = data.Description ?? "";
                if (data.DisplayOrder < 1)
                    ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị phải lớn hơn 0");

                // Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                    string filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "products", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }
                else if (data.PhotoID > 0 && string.IsNullOrWhiteSpace(data.Photo))
                {
                    var oldData = await CatalogDataService.GetPhotoAsync(data.PhotoID);
                    data.Photo = oldData?.Photo;
                }

                if (string.IsNullOrWhiteSpace(data.Photo))
                    ModelState.AddModelError(nameof(data.Photo), "Vui lòng chọn ảnh");

                if (!ModelState.IsValid)
                    return View("EditPhoto", data);

                if (data.PhotoID == 0)
                    await CatalogDataService.AddPhotoAsync(data);
                else
                    await CatalogDataService.UpdatePhotoAsync(data);

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", $"Có lỗi xảy ra: {ex.Message}");
                return View("EditPhoto", data);
            }
        }

        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await CatalogDataService.DeletePhotoAsync(photoId);
                    return RedirectToAction("Edit", new { id });
                }

                var model = await CatalogDataService.GetPhotoAsync(photoId);
                if (model == null || model.ProductID != id)
                    return RedirectToAction("Edit", new { id });

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Edit", new { id });
            }
        }
    }
}
