namespace Qz.Infra.Database.Dto;

public class PageAndOrderByDto : PageQueryDto, IOrderByQuery
{
    public string OrderBy { get; set; }

    public bool IsDesc { get; set; }
}
