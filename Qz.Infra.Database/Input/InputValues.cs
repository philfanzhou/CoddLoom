using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Qz.Infra.Database.Input
{
    public class InputValues
    {
        private readonly Dictionary<string, InputValuesItem> _items = new();

        public IReadOnlyList<InputValuesItem> Items => _items.Values.ToList().AsReadOnly();

        public void Add(string column, string value, DbType type = DbType.String)
        {
            _items.Add(column, new InputValuesItem
            {
                Column = column,
                StringValue = value,
                Type = type
            });
        }

        public void Add(string column, DateTime value)
        {
            _items.Add(column, new InputValuesItem
            {
                Column = column,
                StringValue = value.ToString("yyyy-MM-dd HH:mm:ss"),
                Type = DbType.DateTime
            });
        }

        public void Add(string column, bool value)
        {
            _items.Add(column, new InputValuesItem
            {
                Column = column,
                StringValue = $"{(value ? 1 : 0)}",
                Type = DbType.Boolean
            });
        }
    }
}
