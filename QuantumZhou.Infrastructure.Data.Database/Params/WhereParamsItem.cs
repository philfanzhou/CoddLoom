namespace QuantumZhou.Infrastructure.Data.Database.Params
{
    public class WhereParamsItem
    {
        public WhereParamsItem(string name, string value, string column)
        {
            Name = name;
            Value = value;
            Column = column;
        }

        public WhereParamsItem(string name, string value)
            : this (name, value, name)
        {
        }

        public string Name { get; }

        public string Value { get; }

        public string Column { get; }
    }
}
