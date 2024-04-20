using Qz.Infra.Database.Condition;
using System;

namespace Qz.Infra.Database.Sql;

partial class SqlBuilder
{
    protected internal virtual string GetConnector(WhereConnector whereConnector)
    {
        return whereConnector == WhereConnector.And ? " AND " : " OR ";
    }

    protected internal virtual string GetOperator(WhereOperator whereOperator)
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

    protected internal virtual string GetLikeParamValue(string value)
    {
        if (value.StartsWith("%") || value.EndsWith("%"))
        {
            return value;
        }

        return $"%{value}%";
    }

    protected internal virtual string GetNestedWhere(string whereSql)
    {
        return $"({whereSql})";
    }
}