using Dapper;
using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.Models.Catalog;
using SV22T1020433.Models.Common;

namespace SV22T1020433.DataLayers.SQLServer
{
    /// <summary>
    /// Repository cho Category
    /// </summary>
    public class CategoryRepository : BaseRepository, IGenericRepository<Category>
    {
        public CategoryRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Category>
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using var connection = OpenConnection();

            var whereClause = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : "WHERE (@SearchValue = N'' OR CategoryName LIKE @SearchValue OR Description LIKE @SearchValue)";

            var searchValue = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $"%{input.SearchValue.Trim()}%";

            var sql = $@"
                SELECT COUNT(*) FROM Categories {whereClause};
                SELECT * FROM Categories {whereClause}
                ORDER BY CategoryName
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
            result.DataItems = (await multi.ReadAsync<Category>()).ToList();

            return result;
        }

        public async Task<Category?> GetAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "SELECT * FROM Categories WHERE CategoryID = @CategoryID";
            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
        }

        public async Task<int> AddAsync(Category data)
        {
            using var connection = OpenConnection();
            var sql = @"
                INSERT INTO Categories(CategoryName, Description)
                VALUES(@CategoryName, @Description);
                SELECT @@IDENTITY;
            ";
            return await connection.QuerySingleAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = OpenConnection();
            var sql = @"
                UPDATE Categories
                SET CategoryName = @CategoryName,
                    Description = @Description
                WHERE CategoryID = @CategoryID
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "DELETE FROM Categories WHERE CategoryID = @CategoryID";
            var affectedRows = await connection.ExecuteAsync(sql, new { CategoryID = id });
            return affectedRows > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = OpenConnection();
            var sql = "SELECT COUNT(*) FROM Products WHERE CategoryID = @CategoryID";
            var count = await connection.QuerySingleAsync<int>(sql, new { CategoryID = id });
            return count > 0;
        }
    }
}

