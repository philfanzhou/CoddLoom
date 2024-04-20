using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using System.Data;

namespace Qz.Infra.Database.Condition.Internal;

internal class WhereConditionsItem : WhereConditionsItemBase
{
    public WhereConditionsItem(ColumnValueParameter parameter,
        WhereOperator whereOperator, WhereConnector connector)
    {
        Parameter = parameter;
        WhereOperator = whereOperator;
        WhereConnector = connector;
    }

    public WhereConditionsItem(ColumnValueParameter parameter, DbType castType,
        WhereOperator whereOperator, WhereConnector connector)
        : this(parameter, whereOperator, connector)
    {
        NeedCast = true;
        CastType = castType;
    }

    public override string Column => Parameter.Column;

    public ColumnValueParameter Parameter { get; }

    public WhereOperator WhereOperator { get; protected set; }

    public bool NeedCast { get; }

    public DbType CastType { get; }

    public override string GetWhereString(SqlBuilder builder)
    {
        // update like condition value
        if (WhereOperator == WhereOperator.Like)
        {
            var value = builder.GetLikeParamValue(Parameter.Value.ToString());
            Parameter.Value = value; // refresh for set db parameter value later.
        }

        var whereColumn = Parameter.Column; // do not update column name in parameter, just update in sql.
        if (NeedCast)
        {
            whereColumn = builder.GetCastColumn(Parameter.Column, CastType);
        }

        return $"{whereColumn}{builder.GetOperator(WhereOperator)}{builder.GetParamName(Parameter)}";
    }
}