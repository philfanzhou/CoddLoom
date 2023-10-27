using Qz.Infra.Database.Params;

namespace Qz.Infra.Database.Input;

internal class InputValuesItem : ISqlParameter
{
    public string Column { get; set; }

    public string ParamName => $"V_{Column}";

    public object Value { get; set; }
}