using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Partner;

namespace SV22T1020433.Admin.Controllers
{
    [Authorize]
    public class SupplierController : _BaseController
    {
        private const string SUPPLIER_SEARCH_INPUT = "SupplierSearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH_INPUT);
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
                var result = await PartnerDataService.ListSuppliersAsync(input);
                ApplicationContext.SetSessionData(SUPPLIER_SEARCH_INPUT, input);
                return View(result);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new PagedResult<Supplier>());
            }
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật thông tin Nhà cung cấp";
                var model = await PartnerDataService.GetSupplierAsync(id);
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
        public async Task<IActionResult> SaveData(Supplier data)
        {
            ViewBag.Title = data.SupplierID == 0 ? "Bổ sung Nhà cung cấp" : "Cập nhật thông tin Nhà cung cấp";

            try
            {
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập tên nhà cung cấp");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập Email của nhà cung cấp");
                else if (!(await PartnerDataService.ValidateSupplierEmailAsync(data.Email, data.SupplierID)))
                    ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng bởi nhà cung cấp khác");

                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành");

                data.ContactName = data.ContactName ?? "";
                data.Phone = data.Phone ?? "";
                data.Address = data.Address ?? "";

                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }

                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                }
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
                    await PartnerDataService.DeleteSupplierAsync(id);
                    return RedirectToAction("Index");
                }

                var model = await PartnerDataService.GetSupplierAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.AllowDelete = !(await PartnerDataService.IsUsedSupplierAsync(id));

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
