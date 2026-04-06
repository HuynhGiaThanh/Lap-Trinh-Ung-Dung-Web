using SV22T1020433.DataLayers.Interfaces;
using SV22T1020433.Models.Common;
using SV22T1020433.Models.Partner;

namespace SV22T1020433.BusinessLayers;

/// <summary>
/// Repository rỗng (tạm thời) để đảm bảo solution build được khi DataLayers chưa có implementation.
/// </summary>
internal class EmptyGenericRepository<T> : IGenericRepository<T> where T : class
{
    public Task<int> AddAsync(T data) => Task.FromResult(0);
    public Task<bool> DeleteAsync(int id) => Task.FromResult(false);
    public Task<T?> GetAsync(int id) => Task.FromResult<T?>(null);
    public Task<bool> IsUsedAsync(int id) => Task.FromResult(false);
    public Task<PagedResult<T>> ListAsync(PaginationSearchInput input)
        => Task.FromResult(new PagedResult<T>
        {
            Page = input.Page,
            PageSize = input.PageSize,
            RowCount = 0,
            DataItems = new List<T>()
        });
    public Task<bool> UpdateAsync(T data) => Task.FromResult(false);
}

internal sealed class EmptyCustomerRepository : EmptyGenericRepository<Customer>, ICustomerRepository
{
    public Task<bool> ValidateEmailAsync(string email, int id = 0) => Task.FromResult(true);
}

