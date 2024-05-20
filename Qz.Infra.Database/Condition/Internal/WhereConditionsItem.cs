using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using System.Data;

namespace Qz.Infra.Database.Condition.Internal;

internal class WhereConditionsItem : WhereConditionsItemBase
{
    public WhereConditionsItem(ValueParam parameter,
        WhereOperator whereOperator, WhereConnector connector)
    {
        Parameter = parameter;
        WhereOperator = whereOperator;
        WhereConnector = connector;
    }

    public WhereConditionsItem(ValueParam parameter, DbType castType,
        WhereOperator whereOperator, WhereConnector connector)
        : this(parameter, whereOperator, connector)
    {
        CastType = castType;
    }

    public override string Column => Parameter.Column;

    public ValueParam Parameter { get; }

    public WhereOperator WhereOperator { get; protected set; }

    public DbType? CastType { get; }

    public override string GetWhereString(SqlBuilder builder)
    {
        // update like condition value
        if (WhereOperator == WhereOperator.Like)
        {
            var value = builder.GetLikeParamValue(Parameter.Value.ToString());
            Parameter.Value = value; // refresh for set db parameter value later.
        }

        var whereColumn = Parameter.Column; // do not update column name in parameter, just update in sql.
        if (CastType.HasValue)
        {
            whereColumn = builder.GetCastColumn(Parameter.Column, CastType.Value);
        }

        return $"{whereColumn}{builder.GetOperator(WhereOperator)}{builder.GetParamName(Parameter)}";
    }
}