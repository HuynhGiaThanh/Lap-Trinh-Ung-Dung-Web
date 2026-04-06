using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020433.BusinessLayers;
using System.Security.Claims;

namespace SV22T1020433.Admin.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username = "", string password = "")
        {
            try
            {
                ViewBag.Username = username;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("Error", "Nhập đầy đủ tên đăng nhập và mật khẩu");
                    return View();
                }

                var userAccount = await UserAccountService.AuthorizeAsync(username, password);
                if (userAccount == null)
                {
                    ModelState.AddModelError("Error", "Đăng nhập thất bại (Sai email hoặc mật khẩu)");
                    return View();
                }

                // Kiểm tra xem có phải là nhân viên (không phải Customer thuần túy)
                var roles = userAccount.RoleNames.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(r => r.Trim().ToLower())
                                                .ToList();

                if (roles.Contains("customer") && roles.Count == 1)
                {
                    ModelState.AddModelError("Error", "Tài khoản khách hàng không có quyền truy cập hệ thống quản trị");
                    return View();
                }

                // Đăng nhập thành công, tạo Identity
                var userData = new WebUserData()
                {
                    UserId = userAccount.UserId,
                    UserName = userAccount.UserName,
                    DisplayName = userAccount.DisplayName,
                    Email = userAccount.Email,
                    Photo = userAccount.Photo ?? "nophoto.png",
                    Roles = roles
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, userData.CreatePrincipal());

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", $"Hệ thống gặp lỗi: {ex.Message}");
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin");
                return View();
            }
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");
                return View();
            }

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            // Kiểm tra mật khẩu cũ bằng cách thử authorize
            var user = await UserAccountService.AuthorizeAsync(userData.UserName, oldPassword);
            if (user == null)
            {
                ModelState.AddModelError("oldPassword", "Mật khẩu cũ không chính xác");
                return View();
            }

            bool success = await UserAccountService.ChangePasswordAsync(userData.UserName, CryptHelper.HashMD5(newPassword));
            if (success)
            {
                ViewBag.Message = "Đổi mật khẩu thành công";
                return View();
            }
            else
            {
                ModelState.AddModelError("Error", "Đổi mật khẩu thất bại");
                return View();
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
