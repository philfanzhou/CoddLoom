namespace Qz.Infra.Database.Params;

public class ColumnValueParameter
{
    internal ColumnValueParameter() {}

    public ColumnValueParameter(string column, object value, string paramName)
    {
        Column = column;
        Value = value;
        ParamName = paramName;
    }

    public string Column { get; internal set; }

    public object Value { get; internal set; }

    public string ParamName { get; internal set; }
}