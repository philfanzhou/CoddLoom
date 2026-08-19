using System.Collections.Generic;

namespace Qz.Infra.Database.Model;

public class PageResult<T>
{
    public List<T> Items { get; set; }

    public int PageNumber { get; set; }

    public int TotalPage { get; set; }

    public int TotalCount { get; set; }
}