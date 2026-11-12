using CoddLoom.Common;
using System;
using System.Collections.Generic;

namespace CoddLoom.Condition;

public class OrderByCondition
{
    private readonly List<OrderByItem> _conditions = new ();

    /// <exception cref="ArgumentNullException">
    /// <paramref name="column"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="column"/> is an empty string.
    /// </exception>
    public OrderByCondition(string column, bool descending = false)
    {
        Add(column, descending);
    }

    /// <exception cref="ArgumentException">
    /// Both <paramref name="orderBy"/> and <paramref name="defaultOrderBy"/> are null or empty strings.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="tableType"/> does not define any constant fields to map order-by columns.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="orderBy"/> matches neither the name nor the value of any constant field of
    /// <paramref name="tableType"/> (comparison is <see cref="StringComparison.OrdinalIgnoreCase"/>).
    /// </exception>
    public OrderByCondition(Type tableType, string orderBy,
        string defaultOrderBy = "", bool descending = false)
        : this(GetColumn(tableType, orderBy, defaultOrderBy), descending)
    {
    }

    /// <exception cref="ArgumentNullException">
    /// <paramref name="column"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="column"/> is an empty string.
    /// </exception>
    public void Add(string column, bool descending = false)
    {
        if (column == null) throw new ArgumentNullException(nameof(column));
        if (column.Length == 0)
            throw new ArgumentException(
                "The order-by column must not be an empty string.",
                nameof(column));
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
            throw new ArgumentException(
                "Neither orderBy nor defaultOrderBy provides an order-by column; specify at least one.",
                nameof(defaultOrderBy));
        }

        if (string.IsNullOrEmpty(orderBy))
        {
            return defaultOrderBy;
        }

        var constFields = tableType.GetAllConstField();
        if (constFields == null || constFields.Length == 0)
        {
            throw new ArgumentException(
                $"Type {tableType.Name} does not define any constant fields to map order-by columns.",
                nameof(tableType));
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

        throw new InvalidOperationException($"Can not get column named {orderBy} in table {tableType.Name}");
    }
}

public class OrderByCondition<TTable> : OrderByCondition where TTable : class
{
    /// <exception cref="ArgumentException">
    /// Both <paramref name="orderBy"/> and <paramref name="defaultOrderBy"/> are null or empty strings.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <typeparamref name="TTable"/> does not define any constant fields to map order-by columns.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="orderBy"/> matches neither the name nor the value of any constant field of
    /// <typeparamref name="TTable"/> (comparison is <see cref="StringComparison.OrdinalIgnoreCase"/>).
    /// </exception>
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