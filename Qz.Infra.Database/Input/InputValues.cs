using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Qz.Infra.Database.Input;

public class InputValues
{
    #region Field

    /// <summary>
    /// use dictionary to make sure will not add same column one time.
    /// </summary>
    private readonly Dictionary<string, ValueParam> _items = new();

    private readonly string _paramPrefix;

    #endregion

    public InputValues() : this(0) { }

    internal InputValues(int paramPrefixIndex = 0)
    {
        _paramPrefix = $"V{paramPrefixIndex}_";
    }

    public IReadOnlyList<ValueParam> Items => _items.Values.ToList().AsReadOnly();

    public InputValues Add<T>(string column, T value, bool forceParameter = false)
    {
        if (null == value)
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else if (value is string str)
        {
            AddString(column, str, allowEmpty: true, forceParameter, autoTrim: false);
        }
        else if (value is DateTime time)
        {
            AddDateTime(column, time, allowMinValue: true, forceParameter);
        }
        else
        {
            AddItem(column, value, forceParameter);
        }

        return this;
    }

    public InputValues AddString(string column, string value,
        bool allowEmpty = true, bool forceParameter = false, bool autoTrim = false)
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

        return this;
    }

    public InputValues AddDateTime(string column, DateTime? time, 
        bool allowMinValue = true, bool forceParameter = false)
    {
        if (null == time)
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else if (allowMinValue == false 
            && time.Value <= DateTime.MinValue)
        {
            AddItem(column, DBNull.Value, forceParameter);
        }
        else
        {
            AddItem(column, time.Value, forceParameter);
        }

        return this;
    }

    public InputValues AddNull(string column, bool forceParameter = false)
    {
        AddItem(column, DBNull.Value, forceParameter);
        return this;
    }

    internal bool IsEmpty()
    {
        return _items.Count < 1;
    }

    #region Private Method

    private void AddItem<T>(string column, T value, bool forceParameter = false)
    {
        if(string.IsNullOrEmpty(column) || string.IsNullOrWhiteSpace(column))
        {
            throw new ArgumentNullException(nameof(column));
        }

        var paramName = $"{_paramPrefix}{column}";
        var valueParam = new ValueParam(column, value, paramName, forceParameter);
        _items.Add(column, valueParam);
    }

    #endregion
}