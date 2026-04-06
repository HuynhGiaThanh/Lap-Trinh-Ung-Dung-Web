using Dapper;
using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.Models.Security;
using System.Security.Cryptography;
using System.Text;

namespace SV22T1020433.DataLayers.SQLServer
{
    /// <summary>
    /// Repository cho UserAccount
    /// </summary>
    public class UserAccountRepository : BaseRepository, IUserAccountRepository
    {
        public UserAccountRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = OpenConnection();
            
            // Hash password để so sánh
            var hashedPassword = HashPassword(password);
            
            var sql = @"
                SELECT CAST(e.EmployeeID AS NVARCHAR) AS UserId,
                       e.Email AS UserName,
                       e.FullName AS DisplayName,
                       e.Email,
                       e.Photo,
                       ISNULL(e.RoleNames, '') AS RoleNames
                FROM Employees e
                WHERE e.Email = @UserName AND e.Password = @Password AND (e.IsWorking = 1)
                
                UNION ALL
                
                SELECT CAST(c.CustomerID AS NVARCHAR) AS UserId,
                       c.Email AS UserName,
                       c.CustomerName AS DisplayName,
                       c.Email,
                       NULL AS Photo,
                       'Customer' AS RoleNames
                FROM Customers c
                WHERE c.Email = @UserName AND c.Password = @Password AND (c.IsLocked = 0)
            ";
            
            var result = await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new 
            { 
                UserName = userName, 
                Password = hashedPassword 
            });
            
            return result;
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = OpenConnection();
            var hashedPassword = HashPassword(password);
            
            // Thử update trong Employees trước
            var sqlEmployee = @"
                UPDATE Employees
                SET Password = @Password
                WHERE Email = @UserName
            ";
            var affectedRows = await connection.ExecuteAsync(sqlEmployee, new 
            { 
                UserName = userName, 
                Password = hashedPassword 
            });
            
            if (affectedRows > 0)
                return true;
            
            // Nếu không có trong Employees, thử update trong Customers
            var sqlCustomer = @"
                UPDATE Customers
                SET Password = @Password
                WHERE Email = @UserName
            ";
            affectedRows = await connection.ExecuteAsync(sqlCustomer, new 
            { 
                UserName = userName, 
                Password = hashedPassword 
            });
            
            return affectedRows > 0;
        }

        /// <summary>
        /// Hash password (MD5)
        /// </summary>
        private string HashPassword(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
