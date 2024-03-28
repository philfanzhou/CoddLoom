using Qz.Infra.Database.Params;
using System;
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
        if (null == value)
        {
            AddItem(column, DBNull.Value);
        }
        else if (value is string str)
        {
            Add(column, str);
        }
        else if (value is DateTime time)
        {
            Add(column, time);
        }
        else
        {
            AddItem(column, value);
        }
    }

    public void Add(string column, DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            AddItem(column, DBNull.Value);
        }
        else
        {
            AddItem(column, value);
        }
    }

    public void Add(string column, string value)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
        {
            AddItem(column, DBNull.Value);
        }
        else
        {
            AddItem(column, value.Trim());
        }
    }

    private void AddItem(string column, object value)
    {
        _items.Add(column, new ColumnValueParameter
        {
            Column = column,
            Value = value,
            ParamName = $"V_{column}"
        });
    }

    internal bool IsEmpty()
    {
        return _items.Count < 1;
    }
}