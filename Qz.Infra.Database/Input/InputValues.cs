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

    private readonly string _parameterPrefix;

    public InputValues() : this(0) { }

    internal InputValues(int parameterPrefixIndex = 0)
    {
        _parameterPrefix = $"V{parameterPrefixIndex}_";
    }

    public IReadOnlyList<ColumnValueParameter> Items => _items.Values.ToList().AsReadOnly();

    internal bool IsEmpty => _items.Count < 1;

    public void Add<T>(string column, T value, bool forceParameter = false)
    {
        if (null == value)
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else if (value is string str)
        {
            AddString(column, str, false, forceParameter, autoTrim: true);
        }
        else if (value is DateTime time)
        {
            AddDateTime(column, time, false, forceParameter);
        }
        else
        {
            AddItem(column, value, forceParameter);
        }
    }

    public void AddString(string column, string value,
        bool allowEmpty = false, bool forceParameter = false, bool autoTrim = true)
    {
        if (null == value)
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else if (allowEmpty == false
                 && (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)))
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else
        {
            AddItem(column, autoTrim ? value.Trim() : value, forceParameter);
        }
    }

    public void AddDateTime(string column, DateTime? value, 
        bool allowMinValue = false, bool forceParameter = false)
    {
        if (value == null || (value.Value == DateTime.MinValue && allowMinValue == false))
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else
        {
            AddItem(column, value.Value, forceParameter);
        }
    }

    public void AddNull(string column, bool forceParameter = false)
    {
        AddItem(column, DBNull.Value, forceParameter);
    }

    private void AddItem<T>(string column, T value, bool forceParameter = false)
    {
        if(string.IsNullOrEmpty(column) || string.IsNullOrWhiteSpace(column))
        {
            throw new ArgumentNullException(nameof(column));
        }

        _items.Add(column, new ColumnValueParameter(column, value, $"{_parameterPrefix}{column}", forceParameter));
    }
}