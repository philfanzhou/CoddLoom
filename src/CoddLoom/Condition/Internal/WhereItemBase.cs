using Qz.Infra.Database.Sql;

namespace Qz.Infra.Database.Condition.Internal;

internal abstract class WhereItemBase(WhereConnector connector)
{
    protected WhereItemBase(string column, WhereConnector connector)
        : this(connector)
    {
        Column = column;
    }

    public virtual string Column { get; }

    public WhereConnector WhereConnector { get; } = connector;

    protected internal abstract string ToSql(SqlBuilder builder);
}