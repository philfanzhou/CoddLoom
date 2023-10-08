using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database.Input;

public class InputValues
{
    /// <summary>
    /// use dictionary to make sure will not add same column one time.
    /// </summary>
    private readonly Dictionary<string, IInputValuesItem> _items = new();

    public IReadOnlyList<IInputValuesItem> Items => _items.Values.ToList().AsReadOnly();

    public void Add(string column, object value, DbType dbType)
    {
        switch (dbType)
        {
            case DbType.String:
                Add(column, value as string);
                break;
            case DbType.DateTime:
                Add(column, (DateTime)value);
                break;
            case DbType.Boolean:
                Add(column, (bool)value);
                break;
            case DbType.Int32:
                Add(column, (int)value);
                break;
            case DbType.Int16:
                Add(column, (short)value);
                break;
            case DbType.Decimal:
                Add(column, (decimal)value);
                break;
            case DbType.Double: 
                Add(column, (double)value);
                break;
            default:
                throw new NotSupportedException($"{dbType} not supported.");
        }
    }

    public void Add(string column, string value)
    {
        _items.Add(column, new InputValuesItem<string>
        {
            Column = column,
            Value = value,
            Type = DbType.String
        });
    }

    public void Add(string column, DateTime value)
    {
        _items.Add(column, new InputValuesItem<DateTime>
        {
            Column = column,
            Value = value,
            Type = DbType.DateTime
        });
    }

    public void Add(string column, bool value)
    {
        _items.Add(column, new InputValuesItem<bool>
        {
            Column = column,
            Value = value,
            Type = DbType.Boolean
        });
    }

    public void Add(string column, int value)
    {
        _items.Add(column, new InputValuesItem<int>
        {
            Column = column,
            Value = value,
            Type = DbType.Int32
        });
    }

    public void Add(string column, short value)
    {
        _items.Add(column, new InputValuesItem<short>
        {
            Column = column,
            Value = value,
            Type = DbType.Int16
        });
    }

    public void Add(string column, decimal value)
    {
        _items.Add(column, new InputValuesItem<decimal>
        {
            Column = column,
            Value = value,
            Type = DbType.Decimal
        });
    }

    public void Add(string column, double value)
    {
        _items.Add(column, new InputValuesItem<double>
        {
            Column = column,
            Value = value,
            Type = DbType.Double
        });
    }
}