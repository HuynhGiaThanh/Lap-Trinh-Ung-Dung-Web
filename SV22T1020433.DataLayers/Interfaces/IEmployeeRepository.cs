using SV22T1020433.Models.HR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020433.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Employee
    /// </summary>
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của nhân viên mới
        /// Nếu id <> 0: Kiểm tra email của nhân viên có mã là id
        /// </param>
        /// <returns></returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);

        /// <summary>Đặt mật khẩu cho nhân viên</summary>
        Task<bool> SetPasswordAsync(int employeeID, string passwordHash);

        /// <summary>Lấy danh sách role của nhân viên (chuỗi phân cách bởi dấu phẩy)</summary>
        Task<string?> GetRoleNamesAsync(int employeeID);

        /// <summary>Cập nhật danh sách role của nhân viên</summary>
        Task<bool> UpdateRoleNamesAsync(int employeeID, string roleNames);
    }
}
