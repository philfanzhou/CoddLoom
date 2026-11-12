using CoddLoom.Params;
using CoddLoom.Sql;
using System.Data;

namespace CoddLoom.Condition.Internal;

internal class WhereConditionsNormalItem(ValueParam parameter, WhereOperator whereOperator, WhereConnector connector)
    : WhereItemBase(connector)
{
    public WhereConditionsNormalItem(ValueParam parameter, DbType castType,
        WhereOperator whereOperator, WhereConnector connector)
        : this(parameter, whereOperator, connector)
    {
        CastType = castType;
    }

    public override string Column => Parameter.Column;

    public ValueParam Parameter { get; } = parameter;

    public WhereOperator WhereOperator { get; protected set; } = whereOperator;

    public DbType? CastType { get; }

    protected internal override string ToSql(SqlBuilder builder, WhereBuildContext context)
    {
        // LIKE wrapping is applied to the snapshot handed to the command; the
        // caller's original parameter value is never rewritten.
        var snapshot = context.Snapshot(Parameter, WhereOperator == WhereOperator.Like);

        var whereColumn = Parameter.Column;
        if (CastType.HasValue)
        {
            whereColumn = builder.GetCastColumn(Parameter.Column, CastType.Value);
        }

        return builder.GetNormalCondition(whereColumn, WhereOperator, snapshot);
    }
}