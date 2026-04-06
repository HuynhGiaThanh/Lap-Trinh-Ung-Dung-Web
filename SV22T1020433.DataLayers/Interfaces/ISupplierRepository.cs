using SV22T1020433.Models.Partner;

namespace SV22T1020433.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các chức năng xử lý dữ liệu cho nhà cung cấp
    /// </summary>
    public interface ISupplierRepository : IGenericRepository<Supplier>
    {
        /// <summary>
        /// Kiểm tra xem email của nhà cung cấp có hợp lệ không
        /// </summary>
        Task<bool> ValidateEmailAsync(string email, int id = 0);
    }
}
