using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Admin.Models;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.HR;

namespace SV22T1020433.Admin.Controllers
{
    [Authorize]
    public class EmployeeController : _BaseController
    {
        private const string EMPLOYEE_SEARCH_INPUT = "EmployeeSearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH_INPUT);
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

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            try
            {
                var result = await HRDataService.ListEmployeesAsync(input);
                ApplicationContext.SetSessionData(EMPLOYEE_SEARCH_INPUT, input);
                return View(result);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return View(new PagedResult<Employee>());
            }
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật thông tin nhân viên";
                var model = await HRDataService.GetEmployeeAsync(id);
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
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";
            try
            {
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");
                else if (!(await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID)))
                    ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng bởi nhân viên khác");

                data.Address = data.Address ?? "";
                data.Phone = data.Phone ?? "";

                // Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                    string filePath = Path.Combine(ApplicationContext.WWWRootPath, "images", "employees", fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }
                else if (data.EmployeeID > 0 && string.IsNullOrWhiteSpace(data.Photo))
                {
                    var oldData = await HRDataService.GetEmployeeAsync(data.EmployeeID);
                    data.Photo = oldData?.Photo;
                }

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (data.EmployeeID == 0)
                    await HRDataService.AddEmployeeAsync(data);
                else
                    await HRDataService.UpdateEmployeeAsync(data);

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
                    await HRDataService.DeleteEmployeeAsync(id);
                    return RedirectToAction("Index");
                }

                var model = await HRDataService.GetEmployeeAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.AllowDelete = !(await HRDataService.IsUsedEmployeeAsync(id));
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            try
            {
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                    return RedirectToAction("Index");

                var model = new EmployeeChangePasswordViewModel
                {
                    Employee = employee
                };
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, EmployeeChangePasswordViewModel model)
        {
            try
            {
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                    return RedirectToAction("Index");

                model.Employee = employee;

                if (string.IsNullOrWhiteSpace(model.NewPassword))
                    ModelState.AddModelError(nameof(model.NewPassword), "Vui lòng nhập mật khẩu mới");
                if (model.NewPassword != model.ConfirmPassword)
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "Xác nhận mật khẩu không khớp");

                if (!ModelState.IsValid)
                    return View(model);

                var newHash = CryptHelper.HashMD5(model.NewPassword);
                bool ok = await HRDataService.SetEmployeePasswordAsync(id, newHash);
                if (!ok)
                {
                    ModelState.AddModelError("Error", "Không đổi được mật khẩu. Vui lòng thử lại.");
                    return View(model);
                }

                TempData["Message"] = "Đổi mật khẩu nhân viên thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", $"Có lỗi xảy ra: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            try
            {
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                    return RedirectToAction("Index");

                var roleNames = await HRDataService.GetEmployeeRoleNamesAsync(id) ?? string.Empty;
                var selectedRoles = roleNames
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                var model = new EmployeeRoleViewModel
                {
                    Employee = employee,
                    SelectedRoles = selectedRoles
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, EmployeeRoleViewModel model)
        {
            try
            {
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                    return RedirectToAction("Index");

                var roles = (model.SelectedRoles ?? new List<string>())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var roleNames = string.Join(",", roles);
                await HRDataService.UpdateEmployeeRoleNamesAsync(id, roleNames);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
