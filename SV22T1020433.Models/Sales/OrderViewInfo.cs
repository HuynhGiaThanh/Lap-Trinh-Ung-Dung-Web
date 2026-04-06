namespace SV22T1020433.Models.Sales
{
    /// <summary>
    /// Thông tin của một đơn hàng khi xem chi tiết (DTO)
    /// </summary>
    public class OrderViewInfo : Order
    {
        /// <summary>
        /// Địa chỉ của khách hàng
        /// </summary>
        public string CustomerAddress { get; set; } = "";

        /// <summary>
        /// Mô tả trạng thái đơn hàng
        /// </summary>
        public string StatusDescription => Status.GetDescription();

        /// <summary>
        /// Tổng giá trị đơn hàng
        /// </summary>
        public decimal SumOfPrice { get; set; }
    }
}
