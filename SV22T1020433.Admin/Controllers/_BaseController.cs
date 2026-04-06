using Microsoft.AspNetCore.Mvc;
using SV22T1020433.Models.Common;

namespace SV22T1020433.Admin.Controllers;

/// <summary>
/// Base controller cung cấp hàm tiện ích tạo PaginationSearchInput
/// </summary>
public abstract class _BaseController : Controller
{
    protected const int DefaultPageSize = 10;

    protected PaginationSearchInput CreatePaginationInput(int page, string searchValue)
        => new PaginationSearchInput
        {
            Page = page <= 0 ? 1 : page,
            PageSize = DefaultPageSize,
            SearchValue = searchValue ?? string.Empty
        };
}

