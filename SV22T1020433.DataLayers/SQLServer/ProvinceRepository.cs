using Dapper;
using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.Models.DataDictionary;

namespace SV22T1020433.DataLayers.SQLServer
{
    /// <summary>
    /// Repository cho Province
    /// </summary>
    public class ProvinceRepository : BaseRepository, IDataDictionaryRepository<Province>
    {
        public ProvinceRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<List<Province>> ListAsync()
        {
            using var connection = OpenConnection();
            var sql = @"
                SELECT DISTINCT ProvinceName
                FROM (
                    SELECT Province AS ProvinceName FROM Suppliers WHERE Province IS NOT NULL AND Province != ''
                    UNION
                    SELECT Province AS ProvinceName FROM Customers WHERE Province IS NOT NULL AND Province != ''
                    UNION
                    SELECT DeliveryProvince AS ProvinceName FROM Orders WHERE DeliveryProvince IS NOT NULL AND DeliveryProvince != ''
                ) AS AllProvinces
                ORDER BY ProvinceName
            ";
            var result = await connection.QueryAsync<Province>(sql);
            return result.ToList();
        }
    }
}
