using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using SV22T1020433.Models.Partner;
using SV22T1020433.Models.Security;
using System.Security.Claims;

namespace SV22T1020433.Shop.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập Email và Mật khẩu");
                return View();
            }

            var userAccount = await UserAccountService.AuthorizeAsync(email, password);
            if (userAccount == null)
            {
                ModelState.AddModelError("", "Đăng nhập thất bại. Kiểm tra lại Email hoặc Mật khẩu");
                return View();
            }

            if (!userAccount.RoleNames.Split(',').Any(r => r.Trim().Equals("Customer", StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("", "Tài khoản này không có quyền truy cập vào cửa hàng");
                return View();
            }

            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo ?? "nophoto.png",
                Roles = new List<string> { "Customer" }
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, userData.CreatePrincipal());

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View(new Customer());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer data, string confirmPassword)
        {
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            if (string.IsNullOrWhiteSpace(data.Password) || data.Password != confirmPassword)
            {
                ModelState.AddModelError("Password", "Mật khẩu xác nhận không khớp");
            }

            if (!await PartnerDataService.ValidatelCustomerEmailAsync(data.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng");
            }

            if (!ModelState.IsValid)
                return View(data);

            // Hash password
            data.Password = CryptHelper.HashMD5(data.Password);
            data.IsLocked = false;

            int id = await PartnerDataService.AddCustomerAsync(data);
            if (id > 0)
            {
                TempData["Message"] = "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Đăng ký thất bại. Vui lòng thử lại sau.");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            var customer = await PartnerDataService.GetCustomerAsync(int.Parse(userData.UserId!));
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Customer data)
        {
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();

            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");

            if (!ModelState.IsValid)
                return View(data);

            var currentCustomer = await PartnerDataService.GetCustomerAsync(int.Parse(userData.UserId!));
            if (currentCustomer == null) return RedirectToAction("Login");

            // Cập nhật thông tin (trừ mật khẩu và email để đảm bảo an toàn hoặc xử lý riêng)
            currentCustomer.CustomerName = data.CustomerName;
            currentCustomer.ContactName = data.ContactName;
            currentCustomer.Province = data.Province;
            currentCustomer.Address = data.Address;
            currentCustomer.Phone = data.Phone;

            bool success = await PartnerDataService.UpdateCustomerAsync(currentCustomer);
            if (success)
            {
                TempData["Message"] = "Cập nhật thông tin cá nhân thành công";
                // Cập nhật lại Identity nếu cần (DisplayName)
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", "Cập nhật thất bại.");
            return View(data);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");
                return View();
            }

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            // Kiểm tra mật khẩu cũ
            var user = await UserAccountService.AuthorizeAsync(userData.Email!, oldPassword);
            if (user == null)
            {
                ModelState.AddModelError("oldPassword", "Mật khẩu cũ không chính xác");
                return View();
            }

            bool success = await UserAccountService.ChangePasswordAsync(userData.Email!, CryptHelper.HashMD5(newPassword));
            if (success)
            {
                TempData["Message"] = "Đổi mật khẩu thành công";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", "Đổi mật khẩu thất bại");
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
