namespace Qz.Infra.Database.Condition.Internal;

internal abstract class WhereConditionsItemBase
{
    public abstract string Column { get; }

    public WhereConnecter WhereConnecter { get; protected set; }
}