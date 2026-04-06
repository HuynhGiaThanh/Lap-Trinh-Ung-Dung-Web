using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020433.Models.Common;
using System.Data;

namespace SV22T1020433.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp cơ sở cho các repository
    /// </summary>
    public abstract class BaseRepository
    {
        protected readonly string _connectionString;

        protected BaseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tạo connection đến CSDL
        /// </summary>
        protected IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
