using Qz.Infra.Database.Common;
using System;

namespace Qz.Infra.Database.Condition;

public class OrderByCondition
{
    public OrderByCondition(string column, bool descending = false)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
        Column = column;
        Descending = descending;
    }

    public OrderByCondition(Type tableType, string orderBy,
        string defaultOrderBy = "", bool descending = false)
        : this(GetColumn(tableType, orderBy, defaultOrderBy), descending)
    {
    }

    public string Column { get; }

    public bool Descending { get; }

    private static string GetColumn(Type tableType, string orderBy, string defaultOrderBy)
    {
        if(string.IsNullOrEmpty(orderBy) && string.IsNullOrEmpty(defaultOrderBy))
        {
            throw new ArgumentNullException(nameof(defaultOrderBy));
        }

        if (string.IsNullOrEmpty(orderBy))
        {
            return defaultOrderBy;
        }

        var constFields = tableType.GetAllConstField();
        if (constFields == null || constFields.Length == 0)
        {
            throw new ArgumentNullException(nameof(tableType));
        }

        foreach (var field in constFields)
        {
            var valueObj = field.GetValue(null);
            if (valueObj == null)
            {
                continue;
            }

            var value = valueObj.ToString();
            if (string.Equals(orderBy, field.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(orderBy, value, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        throw new Exception($"Can not get column named {orderBy} in table {tableType.Name}");
    }
}

public class OrderByCondition<TTable> : OrderByCondition where TTable : class
{
    public OrderByCondition(string orderBy, string defaultOrderBy = "", bool descending = false)
        : base(typeof(TTable), orderBy, defaultOrderBy, descending)
    {
    }
}