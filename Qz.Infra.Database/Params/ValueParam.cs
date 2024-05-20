namespace Qz.Infra.Database.Params;

public class ValueParam
{
    public ValueParam(string column, object value, string paramName)
    {
        Column = column;
        Value = value;
        ParamName = paramName;
    }

    internal ValueParam(string column, object value, string paramName, bool forceParameter)
        : this(column, value, paramName)
    {
        ForceParameter = forceParameter;
    }

    public string Column { get; }

    public object Value { get; internal set; }

    public string ParamName { get; internal set; }

    internal bool ForceParameter { get; set; }
}