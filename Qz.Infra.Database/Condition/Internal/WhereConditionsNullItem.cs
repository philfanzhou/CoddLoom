using Qz.Infra.Database.Sql;

namespace Qz.Infra.Database.Condition.Internal;

internal class WhereConditionsNullItem : WhereConditionsItemBase
{
    public WhereConditionsNullItem(string column, bool isNull,
        WhereConnecter whereConnecter = WhereConnecter.And)
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