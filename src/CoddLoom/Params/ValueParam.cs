namespace CoddLoom.Params;

public class ValueParam
{
    public ValueParam(object value, string paramName)
    {
        Value = value;
        ParamName = paramName;
    }

    internal ValueParam(string column, object value, string paramName)
        : this(value, paramName)
    {
        Column = column;
    }

    internal ValueParam(string column, object value, string paramName, bool forceParameter)
        : this(column, value, paramName)
    {
        ForceParameter = forceParameter;
    }

    public string Column { get; }

    public object Value { get; internal set; }

    public string ParamName { get; internal set; }

    internal bool ForceParameter { get; }
}