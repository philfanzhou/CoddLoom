using Qz.Infra.Database.Sql;

namespace Qz.Infra.Database.Condition.Internal;

internal class WhereConditionsNullItem(
    string column,
    bool isNull,
    WhereConnector connector)
    : WhereItemBase(column, connector)
{
    public bool IsNull { get; } = isNull;

    protected internal override string ToSql(SqlBuilder builder)
    {
        return builder.GetIsNullCondition(Column, IsNull);
    }
}