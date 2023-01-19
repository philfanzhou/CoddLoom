using System.Collections.Generic;
using System.Linq;

namespace Qz.Infra.Database.Output
{
    public class PageResult<T>
    {
        internal PageResult()
        {
        }

        internal PageResult(IEnumerable<T> items, int pageIndex, int totalPage, int totalCount)
        {
            Items = items == null ? new List<T>().AsReadOnly() : items.ToList().AsReadOnly();
            PageIndex = pageIndex;
            TotalPage = totalPage;
            TotalCount = totalCount;
        }

        public IReadOnlyList<T> Items { get; }

        public int PageIndex { get; }

        public int TotalPage { get; }

        public int TotalCount { get; }
    }
}
