using SV22T1020433.Models.Common;

namespace SV22T1020433.Models.Sales
{
    /// <summary>
    /// Đầu vào tìm kiếm, phân trang đơn hàng
    /// </summary>
    public class OrderSearchInput : PaginationSearchInput
    {
        /// <summary>
        /// Trạng thái đơn hàng
        /// </summary>
        public OrderStatusEnum Status { get; set; }
        /// <summary>
        /// Từ ngày (ngày lập đơn hàng)
        /// </summary>
        public DateTime? DateFrom { get; set; }
        /// <summary>
        /// Đến ngày (ngày lập đơn hàng)
        /// </summary>
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Khoảng thời gian tìm kiếm (dạng chuỗi: "dd/MM/yyyy - dd/MM/yyyy")
        /// </summary>
        public string DateRange { get; set; } = "";
    }
}
