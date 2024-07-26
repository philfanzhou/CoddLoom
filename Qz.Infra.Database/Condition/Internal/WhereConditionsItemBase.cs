using Qz.Infra.Database.Sql;

namespace Qz.Infra.Database.Condition.Internal;

internal abstract class WhereConditionsItemBase
{
    public abstract string Column { get; }

    public WhereConnector WhereConnector { get; protected set; }

    protected internal abstract string ToSql(SqlBuilder builder);
}