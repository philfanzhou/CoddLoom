namespace Qz.Infra.Database.Params;

public class PageParam
{
    public int PageSize { get; set; }

    public int PageNumber { get; set; }

    internal int Offset => PageSize * (PageNumber - 1);
}