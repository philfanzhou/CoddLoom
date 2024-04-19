using Qz.Infra.Database.Params;
using Qz.Infra.Database.Sql;
using System.Data;

namespace Qz.Infra.Database.Condition.Internal;

internal class WhereConditionsItem : WhereConditionsItemBase
{
    public WhereConditionsItem(ColumnValueParameter parameter,
        WhereOperator whereOperator, WhereConnector connecter)
    {
        Parameter = parameter;
        WhereOperator = whereOperator;
        WhereConnecter = connecter;
    }

    public WhereConditionsItem(ColumnValueParameter parameter, DbType castType,
        WhereOperator whereOperator, WhereConnector connecter)
        : this(parameter, whereOperator, connecter)
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
            Parameter.Value = value;
        }

        var whereColumn = Parameter.Column;
        if (NeedCast)
        {
            whereColumn = builder.GetCastColumn(Parameter.Column, CastType);
        }

        return $"{whereColumn}{builder.GetOperator(WhereOperator)}{builder.GetParamName(Parameter)}";
    }
}