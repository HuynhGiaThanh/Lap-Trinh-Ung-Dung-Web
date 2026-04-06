namespace SV22T1020433.Admin
{
    /// <summary>
    /// Lớp biểu diễn kết quả trả về khi gọi API
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Khởi tạo một kết quả API mới
        /// </summary>
        /// <param name="code">Mã trạng thái (1: Thành công, 0: Thất bại)</param>
        /// <param name="message">Thông báo đi kèm</param>
        public ApiResult(int code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>
        /// Mã kết quả: 0 là Lỗi/Không thành công, 1 là Thành công
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Thông báo chi tiết về kết quả (thông báo lỗi hoặc thông báo thành công)
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// Phương thức tĩnh hỗ trợ tạo nhanh kết quả thành công
        /// </summary>
        public static ApiResult Success(string message = "Thành công") => new ApiResult(1, message);

        /// <summary>
        /// Phương thức tĩnh hỗ trợ tạo nhanh kết quả thất bại
        /// </summary>
        public static ApiResult Fail(string message = "Thất bại") => new ApiResult(0, message);
    }
}
