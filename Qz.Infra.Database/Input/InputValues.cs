using Qz.Infra.Database.Params;
using System.Collections.Generic;
using System.Linq;

namespace Qz.Infra.Database.Input;

public class InputValues
{
    /// <summary>
    /// use dictionary to make sure will not add same column one time.
    /// </summary>
    private readonly Dictionary<string, IDbParam> _items = new();

    internal IReadOnlyList<IDbParam> Items => _items.Values.ToList().AsReadOnly();

    public void Add(string column, object value)
    {
        _items.Add(column, new InputValuesItem
        {
            Column = column,
            Value = value,
        });
    }

    public void Add(string column, string value)
    {
        _items.Add(column, new InputValuesItem
        {
            Column = column,
            Value = value,
        });
    }
}