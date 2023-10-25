using Qz.Infra.Database.Params;
using System.Data;

namespace Qz.Infra.Database.Input;

public class InputValuesItem<T> : IInputValuesItem
{
    public string Column { get; set; }

    public DbType Type { get; set; }

    public T Value { get; set; }

    public bool IsUnicode { get; set; }
}

internal class InputValuesItem : IDbParam
{
    internal string Column { get; set; }

    public string ParamName => $"V_{Column}";

    public object Value { get; set; }
}