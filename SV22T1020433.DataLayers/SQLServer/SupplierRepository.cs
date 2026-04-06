using Dapper;
using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Partner;

namespace SV22T1020433.DataLayers.SQLServer
{
    /// <summary>
    /// Repository cho Supplier
    /// </summary>
    public class SupplierRepository : BaseRepository, ISupplierRepository
    {
        public SupplierRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = OpenConnection();
            int count;
            if (id == 0)
            {
                var sql = "SELECT COUNT(*) FROM Suppliers WHERE Email = @Email";
                count = await connection.QuerySingleAsync<int>(sql, new { Email = email });
            }
            else
            {
                var sql = "SELECT COUNT(*) FROM Suppliers WHERE Email = @Email AND SupplierID != @SupplierID";
                count = await connection.QuerySingleAsync<int>(sql, new { Email = email, SupplierID = id });
            }
            return count == 0;
        }

        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Supplier>
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using var connection = OpenConnection();
            var whereClause = string.IsNullOrWhiteSpace(input.SearchValue) 
                ? "" 
                : "WHERE (@SearchValue = N'' OR SupplierName LIKE @SearchValue OR ContactName LIKE @SearchValue OR Phone LIKE @SearchValue OR Email LIKE @SearchValue)";

            var searchValue = string.IsNullOrWhiteSpace(input.SearchValue) 
                ? "" 
                : $"%{input.SearchValue.Trim()}%";

            var sql = $@"
                SELECT COUNT(*) FROM Suppliers {whereClause};
                SELECT * FROM Suppliers {whereClause}
                ORDER BY SupplierName
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;
            ";

            var parameters = new
            {
                SearchValue = searchValue,
                Offset = input.Offset,
                PageSize = input.PageSize > 0 ? input.PageSize : int.MaxValue
            };

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            result.RowCount = await multi.ReadSingleAsync<int>();
            result.DataItems = (await multi.ReadAsync<Supplier>()).ToList();

            return result;
        }

        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";
            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
        }

        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = OpenConnection();
            var sql = @"
                INSERT INTO Suppliers(SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES(@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                SELECT @@IDENTITY;
            ";
            return await connection.QuerySingleAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = OpenConnection();
            var sql = @"
                UPDATE Suppliers
                SET SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email
                WHERE SupplierID = @SupplierID
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "DELETE FROM Suppliers WHERE SupplierID = @SupplierID";
            var affectedRows = await connection.ExecuteAsync(sql, new { SupplierID = id });
            return affectedRows > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "SELECT COUNT(*) FROM Products WHERE SupplierID = @SupplierID";
            var count = await connection.QuerySingleAsync<int>(sql, new { SupplierID = id });
            return count > 0;
        }
    }
}
