namespace Qz.Infra.Database.Params;

public class PageParam
{
    private int _pageSize;
    private int _pageNumber;

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(value));
            }
            _pageSize = value;
        }
    }

    public int PageNumber
    {
        get => _pageNumber;
        set
        {
            if (value <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(value));
            }
            _pageNumber = value;
        }
    }

    public int Offset => PageSize * (PageNumber - 1);
}