using Dapper;
using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Sales;
using System.Globalization;

namespace SV22T1020433.DataLayers.SQLServer
{
    /// <summary>
    /// Repository cho Order
    /// </summary>
    public class OrderRepository : BaseRepository, IOrderRepository
    {
        public OrderRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            var result = new PagedResult<OrderViewInfo>
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            // Parse DateRange if present
            if (!string.IsNullOrWhiteSpace(input.DateRange))
            {
                string[] dates = input.DateRange.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (dates.Length == 2)
                {
                    if (DateTime.TryParseExact(dates[0], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
                        input.DateFrom = from;
                    if (DateTime.TryParseExact(dates[1], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
                        input.DateTo = to;
                }
            }

            using var connection = OpenConnection();
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                conditions.Add("(@SearchValue = N'' OR o.OrderID LIKE @SearchValue OR c.CustomerName LIKE @SearchValue OR c.Phone LIKE @SearchValue OR s.ShipperName LIKE @SearchValue)");
                parameters.Add("SearchValue", $"%{input.SearchValue.Trim()}%");
            }

            if (input.Status != 0)
            {
                conditions.Add("o.Status = @Status");
                parameters.Add("Status", (int)input.Status);
            }

            if (input.DateFrom.HasValue)
            {
                conditions.Add("CAST(o.OrderTime AS DATE) >= @DateFrom");
                parameters.Add("DateFrom", input.DateFrom.Value.Date);
            }

            if (input.DateTo.HasValue)
            {
                conditions.Add("CAST(o.OrderTime AS DATE) <= @DateTo");
                parameters.Add("DateTo", input.DateTo.Value.Date);
            }

            var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
            parameters.Add("Offset", input.Offset);
            parameters.Add("PageSize", input.PageSize > 0 ? input.PageSize : int.MaxValue);

            var sql = $@"
                SELECT COUNT(*) 
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                {whereClause};
                
                SELECT o.*, 
                       c.CustomerName,
                       c.Phone AS CustomerPhone,
                       e.FullName AS EmployeeName,
                       s.ShipperName,
                       (SELECT SUM(Quantity * SalePrice) FROM OrderDetails WHERE OrderID = o.OrderID) AS SumOfPrice
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                {whereClause}
                ORDER BY o.OrderTime DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;
            ";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            result.RowCount = await multi.ReadSingleAsync<int>();
            result.DataItems = (await multi.ReadAsync<OrderViewInfo>()).ToList();

            return result;
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = OpenConnection();
            var sql = @"
                SELECT o.*,
                       e.FullName AS EmployeeName,
                       c.CustomerName,
                       c.ContactName AS CustomerContactName,
                       c.Email AS CustomerEmail,
                       c.Phone AS CustomerPhone,
                       c.Address AS CustomerAddress,
                       s.ShipperName,
                       s.Phone AS ShipperPhone
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE o.OrderID = @OrderID
            ";
            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
        }

        public async Task<int> AddAsync(Order data)
        {
            using var connection = OpenConnection();
            var sql = @"
                INSERT INTO Orders(CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                VALUES(@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                SELECT @@IDENTITY;
            ";
            return await connection.QuerySingleAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = OpenConnection();
            var sql = @"
                UPDATE Orders
                SET CustomerID = @CustomerID,
                    OrderTime = @OrderTime,
                    DeliveryProvince = @DeliveryProvince,
                    DeliveryAddress = @DeliveryAddress,
                    EmployeeID = @EmployeeID,
                    AcceptTime = @AcceptTime,
                    ShipperID = @ShipperID,
                    ShippedTime = @ShippedTime,
                    FinishedTime = @FinishedTime,
                    Status = @Status
                WHERE OrderID = @OrderID
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                var deleteDetailsSql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID";
                await connection.ExecuteAsync(deleteDetailsSql, new { OrderID = orderID }, transaction);

                var deleteOrderSql = "DELETE FROM Orders WHERE OrderID = @OrderID";
                var affectedRows = await connection.ExecuteAsync(deleteOrderSql, new { OrderID = orderID }, transaction);

                transaction.Commit();
                return affectedRows > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = OpenConnection();
            var sql = @"
                SELECT od.*,
                       p.ProductName,
                       p.Unit,
                       p.Photo
                FROM OrderDetails od
                INNER JOIN Products p ON od.ProductID = p.ProductID
                WHERE od.OrderID = @OrderID
                ORDER BY od.ProductID
            ";
            var result = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID });
            return result.ToList();
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = OpenConnection();
            var sql = @"
                SELECT od.*,
                       p.ProductName,
                       p.Unit,
                       p.Photo
                FROM OrderDetails od
                INNER JOIN Products p ON od.ProductID = p.ProductID
                WHERE od.OrderID = @OrderID AND od.ProductID = @ProductID
            ";
            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID, ProductID = productID });
        }

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = OpenConnection();
            var sql = @"
                INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = OpenConnection();
            var sql = @"
                UPDATE OrderDetails
                SET Quantity = @Quantity,
                    SalePrice = @SalePrice
                WHERE OrderID = @OrderID AND ProductID = @ProductID
            ";
            var affectedRows = await connection.ExecuteAsync(sql, data);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = OpenConnection();
            var sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";
            var affectedRows = await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID });
            return affectedRows > 0;
        }
    }
}
