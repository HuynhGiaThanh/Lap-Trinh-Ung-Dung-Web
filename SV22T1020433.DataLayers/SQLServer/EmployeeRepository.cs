using Dapper;
using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.HR;

namespace SV22T1020433.DataLayers.SQLServer
{
    /// <summary>
    /// Repository cho Employee
    /// </summary>
    public class EmployeeRepository : BaseRepository, IEmployeeRepository
    {
        public EmployeeRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using var connection = OpenConnection();
            var whereClause = string.IsNullOrWhiteSpace(input.SearchValue) 
                ? "" 
                : "WHERE (@SearchValue = N'' OR FullName LIKE @SearchValue OR Phone LIKE @SearchValue OR Email LIKE @SearchValue OR Address LIKE @SearchValue)";

            var searchValue = string.IsNullOrWhiteSpace(input.SearchValue) 
                ? "" 
                : $"%{input.SearchValue.Trim()}%";

            var sql = $@"
                SELECT COUNT(*) FROM Employees {whereClause};
                SELECT * FROM Employees {whereClause}
                ORDER BY FullName
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
            result.DataItems = (await multi.ReadAsync<Employee>()).ToList();

            return result;
        }

        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
        }

        public async Task<int> AddAsync(Employee data)
        {
            using var connection = OpenConnection();
            var sql = @"
                INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                VALUES(@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
                SELECT @@IDENTITY;
            ";
            return await connection.QuerySingleAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = OpenConnection();
            var sql = @"
                UPDATE Employees
                SET FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking
                WHERE EmployeeID = @EmployeeID
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
            var affectedRows = await connection.ExecuteAsync(sql, new { EmployeeID = id });
            return affectedRows > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "SELECT COUNT(*) FROM Orders WHERE EmployeeID = @EmployeeID";
            var count = await connection.QuerySingleAsync<int>(sql, new { EmployeeID = id });
            return count > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = OpenConnection();
            int count;
            
            if (id == 0)
            {
                var sql = "SELECT COUNT(*) FROM Employees WHERE Email = @Email";
                count = await connection.QuerySingleAsync<int>(sql, new { Email = email });
            }
            else
            {
                var sql = "SELECT COUNT(*) FROM Employees WHERE Email = @Email AND EmployeeID != @EmployeeID";
                count = await connection.QuerySingleAsync<int>(sql, new { Email = email, EmployeeID = id });
            }
            
            return count == 0;
        }

        public async Task<bool> SetPasswordAsync(int employeeID, string passwordHash)
        {
            using var connection = OpenConnection();
            string sql = "UPDATE Employees SET Password = @passwordHash WHERE EmployeeID = @employeeID";
            return await connection.ExecuteAsync(sql, new { employeeID, passwordHash }) > 0;
        }

        public async Task<string?> GetRoleNamesAsync(int employeeID)
        {
            using var connection = OpenConnection();
            return await connection.ExecuteScalarAsync<string>(
                "SELECT RoleNames FROM Employees WHERE EmployeeID = @employeeID", new { employeeID });
        }

        public async Task<bool> UpdateRoleNamesAsync(int employeeID, string roleNames)
        {
            using var connection = OpenConnection();
            return await connection.ExecuteAsync(
                "UPDATE Employees SET RoleNames = @roleNames WHERE EmployeeID = @employeeID",
                new { employeeID, roleNames }) > 0;
        }
    }
}
