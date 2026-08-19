namespace CoddLoom.Dto;

public class PageAndOrderByDto : PageQueryDto, IOrderByQuery
{
    public string OrderBy { get; set; }

    public bool IsDesc { get; set; }
}