using System.Collections.Generic;

namespace Qz.Infra.Database.Params;

public class WhereParams
{
    private readonly List<WhereParamsItem> _items = new();

    #region Constructor

    public WhereParams(IEnumerable<WhereParamsItem> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public WhereParams(string name, object value)
        : this(new WhereParamsItem(name, value))
    {
    }

    internal WhereParams(WhereParamsItem item)
    {
        Add(item);
    }

    #endregion

    internal IReadOnlyCollection<WhereParamsItem> Items => _items.AsReadOnly();

    public void Add(string name, object value)
    {
        Add(new WhereParamsItem(name, value));
    }

    internal void Add(WhereParamsItem item)
    {
        _items.Add(item);
    }
}