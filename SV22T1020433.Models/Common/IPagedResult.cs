namespace SV22T1020433.Models.Common
{
    /// <summary>
    /// Giao diện cho kết quả phân trang
    /// </summary>
    public interface IPagedResult
    {
        int Page { get; }
        int PageSize { get; }
        int RowCount { get; }
        int PageCount { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
        List<PageItem> GetDisplayPages(int n = 5);
    }
}
