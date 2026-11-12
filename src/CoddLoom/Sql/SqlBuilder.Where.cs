using CoddLoom.Condition;
using CoddLoom.Condition.Internal;
using CoddLoom.Input;
using CoddLoom.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoddLoom.Sql;

partial class SqlBuilder
{
    private string GetWhereSql(WhereConditions where, WhereBuildContext context)
    {
        var whereBuilder = new StringBuilder();
        AppendWhereItems(where, whereBuilder, context);

        return whereBuilder.ToString();
    }

    private void AppendWhereItems(WhereConditions where, StringBuilder whereBuilder, WhereBuildContext context)
    {
        foreach (var item in where.Items)
        {
            if (whereBuilder.Length > 0)
            {
                whereBuilder.Append(GetConnector(item.WhereConnector));
            }
            whereBuilder.Append(item.ToSql(this, context));
        }
    }

    protected virtual string AppendWhere(string sql,
        WhereConditions where = null)
    {
        if (where == null || where.IsEmpty()) return sql; // Empty conditions must be handled explicitly.
        return $"{sql} WHERE {GetWhereSql(where, new WhereBuildContext(this))}";
    }

    internal string RenderInnerWhereSql(WhereConditions innerWhere, WhereBuildContext context)
    {
        var innerBuilder = new StringBuilder();
        AppendWhereItems(innerWhere, innerBuilder, context);
        return $"({innerBuilder})";
    }

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

    #region Combined SQL and parameter materialization

    /// <summary>
    /// Materializes the parameters of one WHERE render: fresh snapshots with
    /// deterministic names and LIKE-wrapped values, in the exact order the
    /// rendered SQL references them. The condition tree is only read.
    /// </summary>
    internal List<ValueParam> MaterializeWhereParameters(WhereConditions where)
    {
        if (where == null || where.IsEmpty())
        {
            return [];
        }

        var context = new WhereBuildContext(this);
        GetWhereSql(where, context);
        return context.Parameters.ToList();
    }

    internal string DeleteWithParameters(string tableName, WhereConditions where, bool force,
        out List<ValueParam> dbParams)
    {
        var sql = Delete(tableName, where, force);
        dbParams = MaterializeWhereParameters(where);
        return sql;
    }

    internal string UpdateWithParameters(string tableName, InputValues input, WhereConditions where, bool force,
        out List<ValueParam> dbParams)
    {
        var sql = Update(tableName, input, where, force);
        dbParams = [.. input.Items, .. MaterializeWhereParameters(where)];
        return sql;
    }

    internal string CountWithParameters(string tableName, WhereConditions where, ColumnParam columns,
        out List<ValueParam> dbParams)
    {
        var sql = Count(tableName, where, columns);
        dbParams = MaterializeWhereParameters(where);
        return sql;
    }

    internal string SelectWithParameters(string tableName, WhereConditions where, OrderByCondition orderBy,
        PageParam pageParam, ColumnParam columns, out List<ValueParam> dbParams)
    {
        // Calls the virtual Select so provider overrides (for example the SQL
        // Server pagination order-by) participate in dispatch.
        var sql = Select(tableName, where, orderBy, pageParam, columns);
        dbParams = MaterializeWhereParameters(where);
        return sql;
    }

    #endregion
}
