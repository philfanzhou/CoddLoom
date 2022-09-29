using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace QuantumZhou.Infrastructure.Data.Database.Input
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
                Value = value,
                Type = type
            });
        }
    }
}
