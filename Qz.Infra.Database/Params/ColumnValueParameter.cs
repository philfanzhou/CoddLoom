namespace Qz.Infra.Database.Params;

public class ColumnValueParameter
{
    internal ColumnValueParameter() {}

    public string Column { get; internal set; }

    public object Value { get; internal set; }

    public string ParamName { get; internal set; }
}