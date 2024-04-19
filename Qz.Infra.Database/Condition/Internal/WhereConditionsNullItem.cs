using Qz.Infra.Database.Sql;

namespace Qz.Infra.Database.Condition.Internal;

internal class WhereConditionsNullItem : WhereConditionsItemBase
{
    public WhereConditionsNullItem(string column, bool isNull,
        WhereConnector whereConnecter = WhereConnector.And)
    {
        Column = column;
        IsNull = isNull;
        WhereConnecter = whereConnecter;
    }

    public bool IsNull { get; }

    public override string Column { get; }

    public override string GetWhereString(SqlBuilder builder)
    {
        return $"{Column} {builder.GetIsNullCondition(IsNull)}";
    }
}