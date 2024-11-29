using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Params;
using System;

namespace Qz.Infra.Database.Sql;

partial class SqlBuilder
{
    protected virtual string AppendWhere(string sql,
        WhereConditions where = null)
    {
        if (where == null || where.IsEmpty()) return sql; // 必须检查是不是Empty，因为有查询条件只有IsNull的查询条件
        return $"{sql} WHERE {where.GetWhereString(this)}";
    }

    protected internal virtual string GetConnector(WhereConnector whereConnector)
    {
        return whereConnector == WhereConnector.And ? " AND " : " OR ";
    }

    protected internal virtual string GetPartialCondition(WhereConditions conditions)
    {
        return $"({conditions.GetWhereString(this)})";
    }

    protected internal virtual string GetLikeParamValue(string value)
    {
        if (value.StartsWith("%") || value.EndsWith("%"))
        {
            return value;
        }

        return $"%{value}%";
    }

    protected internal virtual string GetNormalCondition(string column, WhereOperator whereOperator, ValueParam valueParam)
    {
        return $"{column}{GetOperator(whereOperator)}{GetParamName(valueParam)}";
    }

    protected virtual string GetOperator(WhereOperator whereOperator)
    {
        return whereOperator switch
        {
            WhereOperator.Equal => " = ",
            WhereOperator.NotEqual => " != ",
            WhereOperator.GreaterThan => " > ",
            WhereOperator.GreaterEqual => " >= ",
            WhereOperator.LessThan => " < ",
            WhereOperator.LessEqual => " <= ",
            WhereOperator.Like => " LIKE ",
            _ => throw new NotSupportedException(nameof(whereOperator)),
        };
    }

    protected internal virtual string GetIsNullCondition(string column, bool isNull)
    {
        return $"{column} IS {(isNull ? "NULL" : "NOT NULL")}";
    }
}