using CoddLoom.Common;
using System;
using System.Collections.Generic;

namespace CoddLoom.Condition;

public class OrderByCondition
{
    private readonly List<OrderByItem> _conditions = new ();

    public OrderByCondition(string column, bool descending = false)
    {
        Add(column, descending);
    }

    public OrderByCondition(Type tableType, string orderBy,
        string defaultOrderBy = "", bool descending = false)
        : this(GetColumn(tableType, orderBy, defaultOrderBy), descending)
    {
    }

    public void Add(string column, bool descending = false)
    {
        if (string.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
        _conditions.Add(new OrderByItem(column, descending));
    }

    internal IEnumerable<OrderByItem> Items => _conditions.AsReadOnly();

    internal bool IsEmpty()
    {
        return _conditions.Count == 0;
    }

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

public class OrderByItem
{
    public OrderByItem(string column, bool descending)
    {
        Column = column;
        Descending = descending;
    }

    public string Column { get; }

    public bool Descending { get; }
}