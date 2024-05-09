namespace Qz.Infra.Database.Params;

public class ColumnValueParameter
{
    public ColumnValueParameter(string column, object value, string paramName)
    {
        Column = column;
        Value = value;
        ParamName = paramName;
    }

    internal ColumnValueParameter(string column, object value, string paramName, bool forceParameter)
        : this(column, value, paramName)
    {
        ForceParameter = forceParameter;
    }

    public string Column { get; }

    public object Value { get; internal set; }

    public string ParamName { get; internal set; }

    internal bool ForceParameter { get; set; }
}