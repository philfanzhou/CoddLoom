namespace Qz.Infra.Database.Params;

public interface ISqlParameter
{
    string Column { get; } // todo: remove it, parameter no need column property.

    string ParamName { get; }

    object Value { get; }
}