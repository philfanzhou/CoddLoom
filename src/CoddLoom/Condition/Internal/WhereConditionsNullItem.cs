using CoddLoom.Sql;

namespace CoddLoom.Condition.Internal;

internal class WhereConditionsNullItem(
    string column,
    bool isNull,
    WhereConnector connector)
    : WhereItemBase(column, connector)
{
    public bool IsNull { get; } = isNull;

    protected internal override string ToSql(SqlBuilder builder, WhereBuildContext context)
    {
        return builder.GetIsNullCondition(Column, IsNull);
    }
}