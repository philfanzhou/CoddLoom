using CoddLoom.Sql;

namespace CoddLoom.Condition.Internal;

internal class PartialWhereConditions(WhereConditions whereConditions, WhereConnector connector)
    : WhereItemBase(connector)
{
    public WhereConditions WhereConditions { get; } = whereConditions;

    protected internal override string ToSql(SqlBuilder builder)
    {
        return builder.RenderInnerWhereSql(WhereConditions);
    }
}
