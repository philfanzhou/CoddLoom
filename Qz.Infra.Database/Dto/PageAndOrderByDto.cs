namespace Qz.Infra.Database.Dto;

public class PageAndOrderByDto : IPageQuery, IOrderByQuery
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string OrderBy { get; set; }

    public bool IsDesc { get; set; }
}
