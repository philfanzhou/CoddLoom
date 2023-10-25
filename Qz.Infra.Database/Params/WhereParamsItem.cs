namespace Qz.Infra.Database.Params;

public class WhereParamsItem : IDbParam
{
    public WhereParamsItem(string paramName, object value, string column)
    {
        ParamName = paramName;
        Value = value;
        Column = column;
    }

    public WhereParamsItem(string paramName, object value)
        : this(paramName, value, paramName) // use param name as column
    {
    }

    public string ParamName { get; set; }

    public object Value { get; set; }

    public string Column { get; set; }
}