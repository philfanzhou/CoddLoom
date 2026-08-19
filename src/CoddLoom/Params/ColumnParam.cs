using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace CoddLoom.Params;

public class ColumnParam
{
    private readonly List<SelectItem> _items = [];

    public ColumnParam AddSelect(string column,
        string dbFunc = null,
        DbType? cast = null,
        string alias = null,
        bool groupBy = false)
    {
        _items.Add(new SelectItem(column, dbFunc, cast, alias, groupBy));
        return this;
    }

    internal IReadOnlyCollection<SelectItem> Select => _items.AsReadOnly();
    internal IReadOnlyCollection<SelectItem> GroupBy => _items.Where(i => i.GroupBy).ToList().AsReadOnly();
}

internal class SelectItem(string column, string dbFunc, DbType? cast, string alias, bool groupBy)
{
    public string Column { get; } = column;
    public string DbFunc { get; } = dbFunc;
    public DbType? Cast { get; } = cast;
    public string Alias { get; } = alias;
    public bool GroupBy { get; } = groupBy;
}