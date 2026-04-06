using Dapper;
using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.Models.Catalog;
using SV22T1020433.Models.Common;

namespace SV22T1020433.DataLayers.SQLServer
{
    /// <summary>
    /// Repository cho Product
    /// </summary>
    public class ProductRepository : BaseRepository, IProductRepository
    {
        public ProductRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            var result = new PagedResult<Product>
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using var connection = OpenConnection();
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                conditions.Add("(@SearchValue = N'' OR ProductName LIKE @SearchValue OR ProductDescription LIKE @SearchValue)");
                parameters.Add("SearchValue", $"%{input.SearchValue.Trim()}%");
            }

            if (input.CategoryID > 0)
            {
                conditions.Add("CategoryID = @CategoryID");
                parameters.Add("CategoryID", input.CategoryID);
            }

            if (input.SupplierID > 0)
            {
                conditions.Add("SupplierID = @SupplierID");
                parameters.Add("SupplierID", input.SupplierID);
            }

            if (input.MinPrice > 0)
            {
                conditions.Add("Price >= @MinPrice");
                parameters.Add("MinPrice", input.MinPrice);
            }

            if (input.MaxPrice > 0)
            {
                conditions.Add("Price <= @MaxPrice");
                parameters.Add("MaxPrice", input.MaxPrice);
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
            parameters.Add("Offset", input.Offset);
            parameters.Add("PageSize", input.PageSize > 0 ? input.PageSize : int.MaxValue);

            var sql = $@"
                SELECT COUNT(*) FROM Products {whereClause};
                SELECT * FROM Products {whereClause}
                ORDER BY ProductName
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;
            ";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            result.RowCount = await multi.ReadSingleAsync<int>();
            result.DataItems = (await multi.ReadAsync<Product>()).ToList();

            return result;
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = OpenConnection();
            var sql = "SELECT * FROM Products WHERE ProductID = @ProductID";
            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = productID });
        }

        public async Task<int> AddAsync(Product data)
        {
            using var connection = OpenConnection();
            var sql = @"
                INSERT INTO Products(ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                VALUES(@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                SELECT @@IDENTITY;
            ";
            return await connection.QuerySingleAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = OpenConnection();
            var sql = @"
                UPDATE Products
                SET ProductName = @ProductName,
                    ProductDescription = @ProductDescription,
                    SupplierID = @SupplierID,
                    CategoryID = @CategoryID,
                    Unit = @Unit,
                    Price = @Price,
                    Photo = @Photo,
                    IsSelling = @IsSelling
                WHERE ProductID = @ProductID
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = OpenConnection();
            var sql = "DELETE FROM Products WHERE ProductID = @ProductID";
            var affectedRows = await connection.ExecuteAsync(sql, new { ProductID = productID });
            return affectedRows > 0;
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = OpenConnection();
            var sql = "SELECT COUNT(*) FROM OrderDetails WHERE ProductID = @ProductID";
            var count = await connection.QuerySingleAsync<int>(sql, new { ProductID = productID });
            return count > 0;
        }

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = OpenConnection();
            var sql = @"
                SELECT * FROM ProductAttributes
                WHERE ProductID = @ProductID
                ORDER BY DisplayOrder, AttributeName
            ";
            var result = await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID });
            return result.ToList();
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = OpenConnection();
            var sql = "SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";
            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = OpenConnection();
            var sql = @"
                INSERT INTO ProductAttributes(ProductID, AttributeName, AttributeValue, DisplayOrder)
                VALUES(@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                SELECT @@IDENTITY;
            ";
            return await connection.QuerySingleAsync<long>(sql, data);
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = OpenConnection();
            var sql = @"
                UPDATE ProductAttributes
                SET ProductID = @ProductID,
                    AttributeName = @AttributeName,
                    AttributeValue = @AttributeValue,
                    DisplayOrder = @DisplayOrder
                WHERE AttributeID = @AttributeID
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = OpenConnection();
            var sql = "DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
            var affectedRows = await connection.ExecuteAsync(sql, new { AttributeID = attributeID });
            return affectedRows > 0;
        }

        public async Task<List<ProductPhoto>> ListPhotoAsync(int productID)
        {
            using var connection = OpenConnection();
            var sql = @"
                SELECT * FROM ProductPhotos
                WHERE ProductID = @ProductID
                ORDER BY DisplayOrder, PhotoID
            ";
            var result = await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID });
            return result.ToList();
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = OpenConnection();
            var sql = "SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";
            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = OpenConnection();
            var sql = @"
                INSERT INTO ProductPhotos(ProductID, Photo, Description, DisplayOrder, IsHidden)
                VALUES(@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                SELECT @@IDENTITY;
            ";
            return await connection.QuerySingleAsync<long>(sql, data);
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = OpenConnection();
            var sql = @"
                UPDATE ProductPhotos
                SET ProductID = @ProductID,
                    Photo = @Photo,
                    Description = @Description,
                    DisplayOrder = @DisplayOrder,
                    IsHidden = @IsHidden
                WHERE PhotoID = @PhotoID
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = OpenConnection();
            var sql = "DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
            var affectedRows = await connection.ExecuteAsync(sql, new { PhotoID = photoID });
            return affectedRows > 0;
        }
    }
}
