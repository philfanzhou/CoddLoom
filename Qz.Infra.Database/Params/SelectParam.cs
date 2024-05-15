using System.Collections.Generic;
using System.Linq;

namespace Qz.Infra.Database.Params;

public class SelectParam
{
    private readonly List<SelectItem> _items = new();

    internal IReadOnlyCollection<SelectItem> Items => _items.AsReadOnly();

    internal IReadOnlyCollection<GroupByItem> GroupBy => _items.Where(p => p.GroupBy).ToList().AsReadOnly();

    public void Add(string column, string alias = null)
    {
        Add(column, null, false, alias);
    }

    public void Add(string column, string dbFunction, bool groupBy, string alias = null)
    {
        _items.Add(new SelectItem(column, alias, dbFunction, groupBy));
    }
}

public class SelectItem : GroupByItem
{
    public SelectItem(string column, string alias, string dbFunction, bool groupBy = false)
        : base(column, groupBy)
    {
        Alias = alias;
        DbFunction = dbFunction;
    }

    public string Alias { get; }

    public string DbFunction { get; }
}

public class GroupByItem
{
    public GroupByItem(string column, bool groupBy = true)
    {
        Column = column;
        GroupBy = groupBy;
    }

    public string Column { get; }

    public bool GroupBy { get; }
}