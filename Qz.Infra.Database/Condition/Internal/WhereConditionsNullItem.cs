using Qz.Infra.Database.Sql;

namespace Qz.Infra.Database.Condition.Internal;

internal class WhereConditionsNullItem : WhereConditionsItemBase
{
    public WhereConditionsNullItem(string column, bool isNull,
        WhereConnector whereConnector = WhereConnector.And)
    {
        Column = column;
        IsNull = isNull;
        WhereConnector = whereConnector;
    }

    public bool IsNull { get; }

    public override string Column { get; }

    public override string GetWhereString(SqlBuilder builder)
    {
        return builder.GetIsNullCondition(Column, IsNull);
    }
}