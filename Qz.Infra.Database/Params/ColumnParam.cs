using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database.Params;

public class ColumnParam
{
    private readonly List<ColumnItemBase> _items = new();

    internal IReadOnlyCollection<SelectItem> Select => _items.OfType<SelectItem>().ToList().AsReadOnly();

    internal IReadOnlyCollection<GroupByItem> GroupBy => _items.OfType<GroupByItem>().Where(p => p.GroupBy).ToList().AsReadOnly();

    public void AddSelect(string column)
    {
        _items.Add(new SelectItem(column));
    }

    public void AddGroupBy(string column)
    {
        _items.Add(new GroupByItem(column));
    }
}

internal class SelectItem : GroupByItem
{
    public SelectItem(string column, DbType? cast = null, string alias = null, bool groupBy = false)
        : base(column, groupBy)
    {
        Alias = alias;
        Cast = cast;
    }

    public string Alias { get; }

    public DbType? Cast { get; }
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