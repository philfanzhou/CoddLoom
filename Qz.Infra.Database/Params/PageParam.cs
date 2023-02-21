namespace Qz.Infra.Database.Params;

public class PageParam
{
    public int PageCount { get; set; }

    public int PageIndex { get; set; }

    internal int Offset => PageCount * PageIndex;
}