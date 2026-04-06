namespace SV22T1020433.BusinessLayers
{
    public class Configuration
    {
        private static string _connectionString = string.Empty;

        /// <summary>
        /// Chuỗi kết nối đến CSDL
        /// </summary>
        public static string ConnectionString => _connectionString;

        /// <summary>
        /// Khởi tạo cấu hình
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
    }
}
