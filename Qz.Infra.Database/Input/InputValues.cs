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

    private readonly string _paramPrefix;

    public InputValues(int paramPrefixIndex = 0)
    {
        _paramPrefix = $"V{paramPrefixIndex}_";
    }

    public IReadOnlyList<ColumnValueParameter> Items => _items.Values.ToList().AsReadOnly();

    public void Add(string column, string value, bool forceEmpty = false, bool forceParameter = false)
    {
        if (forceEmpty == false
            && (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)))
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else
        {
            AddItem(column, value.Trim(), forceParameter);
        }
    }

    public void Add(string column, DateTime value, bool forceParameter = false)
    {
        if (value == DateTime.MinValue)
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else
        {
            AddItem(column, value, forceParameter);
        }
    }

    public void Add(string column, object value, bool forceParameter = false)
    {
        if (null == value)
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else if (value is string str)
        {
            Add(column, str, false, forceParameter);
        }
        else if (value is DateTime time)
        {
            Add(column, time, forceParameter);
        }
        else
        {
            AddItem(column, value, forceParameter);
        }
    }

    private void AddItem(string column, object value, bool forceParameter = false)
    {
        _items.Add(column, new ColumnValueParameter(column, value, $"{_paramPrefix}{column}", forceParameter));
    }

    internal bool IsEmpty()
    {
        return _items.Count < 1;
    }
}