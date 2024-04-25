using Qz.Infra.Database.Condition;

namespace Qz.Infra.Database.Dto;

public class OrderByDto : IOrderByQuery
{
    public string OrderBy { get; set; }

    public bool IsDesc { get; set; }
}

public interface IOrderByQuery
{
    string OrderBy { get; }

    bool IsDesc { get; }
}

public static class IOrderByQueryExt
{
    public static OrderByCondition GetOrderByCondition<TTable>(this IOrderByQuery self,
        string defaultOrderBy = "")
        where TTable : class
    {
        return new OrderByCondition<TTable>(self.OrderBy, defaultOrderBy, self.IsDesc);
    }
}
