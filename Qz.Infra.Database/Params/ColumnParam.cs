using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database.Params;

public class ColumnParam
{
    private readonly List<ColumnItemBase> _items = new();

    internal IReadOnlyCollection<SelectItem> Select => _items.OfType<SelectItem>().ToList().AsReadOnly();

    internal IReadOnlyCollection<GroupByItem> GroupBy => _items.OfType<GroupByItem>().Where(p => p.GroupBy).ToList().AsReadOnly();

    public void AddSelect(string column, 
        string dbFunc = null, DbType? cast = null, string alias = null, bool groupBy = false)
    {
        _items.Add(new SelectItem(column, dbFunc, cast, alias, groupBy));
    }

    public void AddGroupBy(string column)
    {
        _items.Add(new GroupByItem(column, true));
    }
}

internal class SelectItem : GroupByItem
{
    public SelectItem(string column, string dbFunc, DbType? cast, string alias, bool groupBy)
        : base(column, groupBy)
    {
        DbFunc = dbFunc;
        Cast = cast;
        Alias = alias;
    }

    public string DbFunc { get; }

    public DbType? Cast { get; }

    public string Alias { get; }
}

internal class GroupByItem : ColumnItemBase
{
    public GroupByItem(string column, bool groupBy)
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