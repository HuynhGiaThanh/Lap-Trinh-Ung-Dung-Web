using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Partner;

namespace SV22T1020433.Admin.Controllers
{
    [Authorize]
    public class CustomerController : _BaseController
    {
        private const string CUSTOMER_SEARCH_INPUT = "CustomerSearchInput";

        /// <summary>
        /// Giao diện chính: Nhập điều kiện tìm kiếm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH_INPUT);
            if (input == null)
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }
            return View(input);
        }

        /// <summary>
        /// Thực hiện tìm kiếm và trả về kết quả dạng PartialView
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH_INPUT, input);
            return View(result);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete = !await PartnerDataService.IsUsedCustomerAsync(id);
            return View(model);
        }

        public IActionResult ChangePassword(int id) => View();

        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            try
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";

                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên khách hàng");
                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Hãy cho biết Email của khách hàng");
                else if (!await PartnerDataService.ValidatelCustomerEmailAsync(data.Email, data.CustomerID))
                    ModelState.AddModelError(nameof(data.Email), "Email này đã có người sử dụng");
                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");

                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }

                //Điều chỉnh dữ liệu theo logic/qui ước của hệ thống
                data.ContactName = data.ContactName ?? "";
                data.Phone = data.Phone ?? "";
                data.Address = data.Address ?? "";

                // Lưu dữ liệu vào CSDL
                if (data.CustomerID == 0)
                    await PartnerDataService.AddCustomerAsync(data);
                else
                    await PartnerDataService.UpdateCustomerAsync(data);

                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError("", "Hệ thống đang bận, vui lòng thử lại sau!");
                return View("Edit", data);
            }
        }
    }
}
