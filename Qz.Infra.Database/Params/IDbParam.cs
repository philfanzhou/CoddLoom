namespace Qz.Infra.Database.Params;

public interface IDbParam
{
    string ParamName { get; }

    object Value { get; }
}