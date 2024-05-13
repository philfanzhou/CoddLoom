using System.Collections.Generic;

namespace Qz.Infra.Database.Params;

public class SelectParam
{
    private readonly List<SelectParamItem> _items = new();

    internal IReadOnlyCollection<SelectParamItem> Items => _items.AsReadOnly();

    public void Add(string column, string alias = null)
    {
        Add(column, null, false, alias);
    }

    public void Add(string column, string dbFunction, bool groupBy, string alias = null)
    {
        _items.Add(new SelectParamItem
        {
            Column = column,
            DbFunction = dbFunction,
            GroupBy = groupBy,
            Alias = alias
        });
    }
}

public class SelectParamItem
{
    public string Column { get; internal set; } 
    
    public string Alias { get; internal set; }

    public string DbFunction { get; internal set; }

    public bool GroupBy { get; internal set; }
}