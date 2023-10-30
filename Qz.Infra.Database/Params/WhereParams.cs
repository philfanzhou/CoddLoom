using System.Collections.Generic;

namespace Qz.Infra.Database.Params;

internal class WhereParams
{
    private readonly List<WhereParamsItem> _items = new();

    #region Constructor

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