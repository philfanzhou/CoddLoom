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
    private readonly Dictionary<string, IInputValuesItem> _sqlItems = new();

    private readonly Dictionary<string, InputValuesItem> _paramItems = new();

    internal IReadOnlyList<IInputValuesItem> SqlItems => _sqlItems.Values.ToList().AsReadOnly();

    internal IReadOnlyList<InputValuesItem> ParamItems => _paramItems.Values.ToList().AsReadOnly();

    public void Add(string column, object value, DbType dbType)
    {
        switch (dbType)
        {
            case DbType.String:
                Add(column, value as string);
                break;
            case DbType.DateTime:
            case DbType.Boolean:
            case DbType.Int32:
            case DbType.Int16:
            case DbType.Decimal:
            case DbType.Double: 
            case DbType.Binary:
                _paramItems.Add(column, new InputValuesItem
                {
                    Column = column,
                    Value = value,
                });
                break;
            default:
                throw new NotSupportedException($"{dbType} not supported.");
        }
    }

    public void Add(string column, string value, bool isUnicode = false)
    {
        _sqlItems.Add(column, new InputValuesItem<string>
        {
            Column = column,
            Value = value,
            Type = DbType.String,
            IsUnicode = isUnicode
        });
    }
}