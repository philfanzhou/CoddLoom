namespace Qz.Infra.Database.Params;

internal class WhereParamsItem : ISqlParameter
{
    /// <summary>
    /// Use column as parameter name.
    /// </summary>
    /// <param name="column"></param>
    /// <param name="value"></param>
    public WhereParamsItem(string column, object value)
        : this(column, value, column)
    {
    }

    public WhereParamsItem(string column, object value, string paramName)
    {
        Column = column;
        Value = value;
        ParamName = paramName;
    }

    public string Column { get; set; }

    public string ParamName { get; set; }

    public object Value { get; set; }
}