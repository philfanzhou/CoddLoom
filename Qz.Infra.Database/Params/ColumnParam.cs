using System.Collections.Generic;
using System.Linq;

namespace Qz.Infra.Database.Params;

public class ColumnParam
{
    private readonly List<ColumnItemBase> _items = new();

    internal IReadOnlyCollection<SelectItem> Select => _items.OfType<SelectItem>().ToList().AsReadOnly();

    internal IReadOnlyCollection<GroupByItem> GroupBy => _items.OfType<GroupByItem>().Where(p => p.GroupBy).ToList().AsReadOnly();

    public void Add(string column, string alias = null)
    {
        Add(column, null, false, alias);
    }

    public void Add(string column, string dbFunction, bool groupBy, string alias = null)
    {
        _items.Add(new SelectItem(column, alias, dbFunction, groupBy));
    }
}

internal class SelectItem : GroupByItem
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

internal class GroupByItem : ColumnItemBase
{
    public GroupByItem(string column, bool groupBy = true)
    {
        Column = column;
        GroupBy = groupBy;
    }

    public override string Column { get; }

    internal bool GroupBy { get; }
}

internal abstract class ColumnItemBase
{
    public abstract string Column { get; }
}