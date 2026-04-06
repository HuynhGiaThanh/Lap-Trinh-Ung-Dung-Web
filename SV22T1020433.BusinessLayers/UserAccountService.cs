using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.DataLayers.SQLServer;
using SV22T1020433.Models.Security;

namespace SV22T1020433.BusinessLayers
{
    /// <summary>
    /// Các nghiệp vụ liên quan đến tài khoản
    /// </summary>
    public static class UserAccountService
    {
        private static readonly IUserAccountRepository userAccountDB;

        static UserAccountService()
        {
            userAccountDB = new UserAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Đăng nhập
        /// </summary>
        public static async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            return await userAccountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        public static async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            return await userAccountDB.ChangePasswordAsync(userName, password);
        }
    }
}
