using Qz.Infra.Database.Params;
using System.Collections.Generic;
using System.Linq;

namespace Qz.Infra.Database.Input;

public class InputValues
{
    /// <summary>
    /// use dictionary to make sure will not add same column one time.
    /// </summary>
    private readonly Dictionary<string, ColumnValueParameter> _items = new();

    public IReadOnlyList<ColumnValueParameter> Items => _items.Values.ToList().AsReadOnly();

    public void Add(string column, object value)
    {
        _items.Add(column, new ColumnValueParameter
        {
            Column = column,
            Value = value ?? System.DBNull.Value,
            ParamName = $"V_{column}"
        });
    }

    internal bool IsEmpty()
    {
        return _items.Count < 1;
    }
}