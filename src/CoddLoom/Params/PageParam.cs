namespace CoddLoom.Params;

public class PageParam
{
    private int _pageSize = 20;
    private int _pageNumber = 1;

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

    public int Offset => checked(PageSize * (PageNumber - 1));
}
