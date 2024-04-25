using System.Reflection;
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

    public OrderByCondition(IReflect tableType, string orderBy,
        string defaultOrderBy = "", bool descending = false)
        : this(GetColumn(tableType, orderBy, defaultOrderBy), descending)
    {
    }

    public string Column { get; }

    public bool Descending { get; }

    private static string GetColumn(IReflect tableType, string orderBy, string defaultOrderBy)
    {
        if(string.IsNullOrEmpty(orderBy) && string.IsNullOrEmpty(defaultOrderBy))
        {
            throw new ArgumentNullException(nameof(defaultOrderBy));
        }

        if (string.IsNullOrEmpty(orderBy))
        {
            return defaultOrderBy;
        }

        var fields = tableType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (fields == null || fields.Length == 0)
        {
            throw new ArgumentNullException(nameof(tableType));
        }

        foreach (var field in fields)
        {
            if (field.IsLiteral && !field.IsInitOnly)
            {
                var valueObj = field.GetValue(null);
                if (valueObj == null)
                {
                    continue;
                }

                var column = valueObj.ToString();
                if (string.Equals(orderBy, column, StringComparison.CurrentCultureIgnoreCase))
                {
                    return column;
                }
            }
        }

        return string.Empty;
    }
}

public class OrderByCondition<TTable> : OrderByCondition where TTable : class
{
    public OrderByCondition(string orderBy, string defaultOrderBy = "", bool descending = false)
        : base(typeof(TTable), orderBy, defaultOrderBy, descending)
    {
    }
}