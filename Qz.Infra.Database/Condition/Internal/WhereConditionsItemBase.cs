using Qz.Infra.Database.Sql;

namespace Qz.Infra.Database.Condition.Internal;

internal abstract class WhereConditionsItemBase
{
    public abstract string Column { get; }

    public WhereConnector WhereConnecter { get; protected set; }

    public abstract string GetWhereString(SqlBuilder builder);
}