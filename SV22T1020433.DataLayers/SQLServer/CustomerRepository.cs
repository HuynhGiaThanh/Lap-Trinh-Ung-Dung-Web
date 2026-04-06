using Dapper;
using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Partner;

namespace SV22T1020433.DataLayers.SQLServer
{
    /// <summary>
    /// Repository cho Customer
    /// </summary>
    public class CustomerRepository : BaseRepository, ICustomerRepository
    {
        public CustomerRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Customer>
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using var connection = OpenConnection();
            var whereClause = string.IsNullOrWhiteSpace(input.SearchValue) 
                ? "" 
                : "WHERE (@SearchValue = N'' OR CustomerName LIKE @SearchValue OR ContactName LIKE @SearchValue OR Phone LIKE @SearchValue OR Email LIKE @SearchValue)";

            var searchValue = string.IsNullOrWhiteSpace(input.SearchValue) 
                ? "" 
                : $"%{input.SearchValue.Trim()}%";

            var sql = $@"
                SELECT COUNT(*) FROM Customers {whereClause};
                SELECT * FROM Customers {whereClause}
                ORDER BY CustomerName
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
            result.DataItems = (await multi.ReadAsync<Customer>()).ToList();

            return result;
        }

        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "SELECT * FROM Customers WHERE CustomerID = @CustomerID";
            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerID = id });
        }

        public async Task<int> AddAsync(Customer data)
        {
            using var connection = OpenConnection();
            var sql = @"
                INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, @IsLocked);
                SELECT @@IDENTITY;
            ";
            return await connection.QuerySingleAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = OpenConnection();
            var sql = @"
                UPDATE Customers
                SET CustomerName = @CustomerName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Password = @Password,
                    IsLocked = @IsLocked
                WHERE CustomerID = @CustomerID
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "DELETE FROM Customers WHERE CustomerID = @CustomerID";
            var affectedRows = await connection.ExecuteAsync(sql, new { CustomerID = id });
            return affectedRows > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "SELECT COUNT(*) FROM Orders WHERE CustomerID = @CustomerID";
            var count = await connection.QuerySingleAsync<int>(sql, new { CustomerID = id });
            return count > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = OpenConnection();
            int count;
            
            if (id == 0)
            {
                var sql = "SELECT COUNT(*) FROM Customers WHERE Email = @Email";
                count = await connection.QuerySingleAsync<int>(sql, new { Email = email });
            }
            else
            {
                var sql = "SELECT COUNT(*) FROM Customers WHERE Email = @Email AND CustomerID != @CustomerID";
                count = await connection.QuerySingleAsync<int>(sql, new { Email = email, CustomerID = id });
            }
            
            return count == 0;
        }
    }
}
