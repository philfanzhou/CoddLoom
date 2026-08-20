using CoddLoom.Condition;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoddLoom.Sql;

partial class SqlBuilder
{
    private string GetWhereSql(WhereConditions where, bool refreshParameterNames = true)
    {
        if (refreshParameterNames)
        {
            where.RefreshParamNames();
        }

        var whereBuilder = new StringBuilder();
        foreach (var item in where.Items)
        {
            if (whereBuilder.Length > 0)
            {
                whereBuilder.Append(GetConnector(item.WhereConnector));
            }
            whereBuilder.Append(item.ToSql(this));
        }

        return whereBuilder.ToString();
    }

    protected virtual string AppendWhere(string sql,
        WhereConditions where = null)
    {
        if (where == null || where.IsEmpty()) return sql; // Empty conditions must be handled explicitly.
        return $"{sql} WHERE {GetWhereSql(where)}";
    }

    protected virtual string GetInnerWhereSql(WhereConditions innerWhere)
    {
        return $"({GetWhereSql(innerWhere, false)})";
    }

    internal string RenderInnerWhereSql(WhereConditions innerWhere) => GetInnerWhereSql(innerWhere);

    protected virtual string GetConnector(WhereConnector whereConnector)
    {
        return whereConnector == WhereConnector.And ? " AND " : " OR ";
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
        var operatorSql = whereOperator switch
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
        return $"{column}{operatorSql}{GetParamName(valueParam)}";
    }

    protected internal virtual string GetIsNullCondition(string column, bool isNull)
    {
        return $"{column} IS {(isNull ? "NULL" : "NOT NULL")}";
    }

    protected internal virtual string GetInCondition(string column, IEnumerable<ValueParam> valueParams)
    {
        var paramNames = string.Join(",", valueParams.Select(GetParamName));
        return $"{column} IN ({paramNames})";
    }
}
