using Qz.Infra.Database.Params;

namespace Qz.Infra.Database.Input;

internal class InputValuesItem : IDbParam
{
    public string ParamName => $"V_{Column}";

    public object Value { get; set; }

    public string Column { get; set; }
}