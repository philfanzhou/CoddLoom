namespace Qz.Infra.Database.Params
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
            : this(name, value, name)
        {
        }

        public WhereParamsItem(string name, bool value)
            : this(name, value ? "1" : "0", name)
        {
        }

        public string Name { get; }

        public string Value { get; set; }

        public string Column { get; }
    }
}
