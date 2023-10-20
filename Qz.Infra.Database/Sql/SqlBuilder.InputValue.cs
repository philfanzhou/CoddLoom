using Qz.Infra.Database.Input;
using System;
using System.Data;

namespace Qz.Infra.Database.Sql;

partial class SqlBuilder
{
    protected virtual string ToValueSql(IInputValuesItem item)
    {
        switch (item.Type)
        {
            case DbType.String:
                return GetStringInputValue(item as InputValuesItem<string>);
            case DbType.DateTime:
                return GetDateTimeInputValue(item as InputValuesItem<DateTime>);
            case DbType.Boolean:
                return GetBoolInputValue(item as InputValuesItem<bool>);
            case DbType.Int32:
                return GetInt32InputValue(item as InputValuesItem<int>);
            case DbType.Int16:
                return GetInt16InputValue(item as InputValuesItem<short>);
            case DbType.Decimal:
                return GetDecimalInputValue(item as InputValuesItem<decimal>);
            default:
                throw new ArgumentOutOfRangeException(nameof(item.Type), item.Type, null);
        }
    }

    protected virtual string GetStringInputValue(InputValuesItem<string> item)
    {
        var value = string.IsNullOrEmpty(item.Value) ? string.Empty : item.Value;
        return $"'{value}'";
    }

    protected virtual string GetDateTimeInputValue(InputValuesItem<DateTime> item)
    {
        return $"'{item.Value:yyyy-MM-dd HH:mm:ss}'";
    }

    protected virtual string GetBoolInputValue(InputValuesItem<bool> item)
    {
        return $"{(item.Value ? 1 : 0)}";
    }

    protected virtual string GetInt32InputValue(InputValuesItem<int> item)
    {
        return $"{item.Value}";
    }

    protected virtual string GetInt16InputValue(InputValuesItem<short> item)
    {
        return $"{item.Value}";
    }

    protected virtual string GetDecimalInputValue(InputValuesItem<decimal> item)
    {
        return $"{item.Value}";
    }
}