using System.Collections.Generic;

namespace QuantumZhou.Infrastructure.Data.Database.Params
{
    public class WhereParams
    {
        private readonly List<WhereParamsItem> _items = new();

        public WhereParams(WhereParamsItem item)
        {
            Add(item);
        }

        public WhereParams(string name, string value)
            : this(new WhereParamsItem(name, value))
        {
        }

        public WhereParams(string name, string value, string column)
            : this(new WhereParamsItem(name, value, column))
        {

        }

        public IReadOnlyCollection<WhereParamsItem> Items => _items.AsReadOnly();

        public void Add(string name, string value)
        {
            Add(new WhereParamsItem(name, value));
        }

        public void Add(WhereParamsItem item)
        {
            _items.Add(item);
        }
    }
}
