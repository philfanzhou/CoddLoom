using CoddLoom.Params;

namespace CoddLoom.Dto;

public class PageQueryDto : IPageQuery
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public interface IPageQuery
{
    int PageNumber { get; }

    int PageSize { get; }
}

public static class PageQueryExt
{
    public static PageParam GetPageParam(this IPageQuery self)
    {
        return new PageParam
        {
            PageNumber = self.PageNumber,
            PageSize = self.PageSize
        };
    }
}