using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using System;
using System.Data;
using System.Text;

namespace Qz.Infra.Database.Sql;

public partial class SqlBuilder
{
    private const string ParamPrefix = "@";
    protected const string KeyWordSelect = "SELECT";

    public virtual string Insert(string tableName, InputValues input)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (input == null || input.IsEmpty()) throw new ArgumentNullException(nameof(input));

        var columnBuilder = new StringBuilder();
        var valueBuilder = new StringBuilder();
        foreach (var item in input.Items)
        {
            if (columnBuilder.Length > 0)
            {
                columnBuilder.Append(",");
            }
            columnBuilder.Append(item.Column);

            if (valueBuilder.Length > 0)
            {
                valueBuilder.Append(",");
            }
            valueBuilder.Append(GetParamName(item));
        }

        return $"INSERT INTO {tableName} ({columnBuilder}) VALUES({valueBuilder})";
    }

    public virtual string Delete(string tableName, WhereConditions where)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (where == null || where.IsEmpty()) throw new ArgumentNullException(nameof(where));

        var sql = $"DELETE FROM {tableName}";
        return AppendWhere(sql, where);
    }

    public virtual string Update(string tableName, InputValues input, WhereConditions where)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (input == null || input.IsEmpty()) throw new ArgumentNullException(nameof(input));
        if (where == null || where.IsEmpty()) throw new ArgumentNullException(nameof(where));

        var valueBuilder = new StringBuilder();
        foreach (var item in input.Items)
        {
            if (valueBuilder.Length > 0)
            {
                valueBuilder.Append(", ");
            }
            valueBuilder.Append($"{item.Column} = ");
            valueBuilder.Append(GetParamName(item));
        }

        var sql = $"UPDATE {tableName} SET {valueBuilder}";
        return AppendWhere(sql, where);
    }

    public virtual string Select(string tableName,
        WhereConditions where = null, OrderByCondition orderBy = null)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

        var sql = $"{KeyWordSelect} * FROM {tableName}";
        sql = AppendWhere(sql, where);
        return AppendOrderBy(sql, orderBy);
    }

    public virtual string Count(string tableName,
        WhereConditions where = null)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

        var column = "*";
        //if (where != null && !where.IsEmpty())
        //{
        //    // 只查询where条件的第一个column，提高性能
        //    column = where.Items.First().Column;
        //}

        var sql = $"SELECT COUNT({column}) FROM {tableName}";
        return AppendWhere(sql, where);
    }

    public virtual string First(string tableName,
        WhereConditions where = null, OrderByCondition orderBy = null)
    {
        var selectSql = Select(tableName, where, orderBy);
        return AppendLimit(selectSql, 1);
    }

    public virtual string Take(string tableName, int offset, int count,
        WhereConditions where = null, OrderByCondition orderBy = null)
    {
        var selectSql = Select(tableName, where, orderBy);
        return AppendLimit(selectSql, count, offset);
    }

    #region Protected virtual

    protected virtual string AppendWhere(string sql,
        WhereConditions where = null)
    {
        if (where == null || where.IsEmpty())
        {
            return sql;
        }

        return $"{sql} WHERE {GetConditionString(where)}";
    }

    protected virtual string AppendOrderBy(string sql,
        OrderByCondition orderBy = null)
    {
        if (orderBy == null) return sql;

        var sort = orderBy.Descending ? "DESC" : "ASC";
        return $"{sql} ORDER BY {orderBy.Column} {sort}";
    }

    protected virtual string AppendLimit(string sql, int count,
        int offset = 0)
    {
        if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

        return $"{sql} LIMIT {offset},{count}";
    }

    protected virtual string GetConnecter(WhereConnecter whereConnecter)
    {
        return whereConnecter == WhereConnecter.And ? " AND " : " OR ";
    }

    protected internal virtual string GetParamName(ColumnValueParameter param)
    {
        return $"{ParamPrefix}{param.ParamName}";
    }

    protected internal virtual string GetCastColumn(string column, DbType dbType)
    {
        var typeStr = dbType switch
        {
            DbType.DateTime => "DateTime",
            _ => throw new NotSupportedException(dbType.ToString())
        };
        return $"CAST({column} AS {typeStr})";
    }

    #endregion

    #region WhereCondition

    protected internal virtual string GetIsNullCondition(bool isNull)
    {
        return $"IS {(isNull ? "NULL" : "NOT NULL")}";
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

    protected internal virtual string GetLikeParamValue(string value)
    {
        if (!value.StartsWith("%"))
        {
            value = $"%{value}";
        }

        if (!value.EndsWith("%"))
        {
            value = $"{value}%";
        }

        return value;
    }

    private string GetConditionString(WhereConditions where)
    {
        var whereBuilder = new StringBuilder();
        foreach (var item in where.Items)
        {
            if (whereBuilder.Length > 0)
            {
                whereBuilder.Append(GetConnecter(item.WhereConnecter));
            }
            whereBuilder.Append(item.GetWhereString(this));
        }

        foreach (var condition in where.PartialItems)
        {
            if (whereBuilder.Length > 0)
            {
                whereBuilder.Append(GetConnecter(condition.Item2));
            }
            whereBuilder.Append($"({GetConditionString(condition.Item1)})");
        }

        return whereBuilder.ToString();
    }

    #endregion
}