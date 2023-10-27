using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using System;
using System.Text;

namespace Qz.Infra.Database.Sql;

public partial class SqlBuilder
{
    protected const string KeyWordSelect = "SELECT";

    protected internal string ParamPrefix { get; set; } = "@";

    public virtual string Insert(string tableName, InputValues input)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (input == null || input.Items.Count < 1) throw new ArgumentNullException(nameof(input));

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
        if (where == null || where.Items.Count < 1) throw new ArgumentNullException(nameof(where));

        var sql = $"DELETE FROM {tableName}";
        return AppendWhere(sql, where);
    }

    public virtual string Update(string tableName, InputValues input, WhereConditions where)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (where == null) throw new ArgumentNullException(nameof(where));

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
        if (where != null && where.Items.Count > 0)
        {
            // 只查询where条件的第一个column，提高性能
            column = where.Items[0].Column;
        }

        var sql = $"SELECT COUNT({column}) FROM {tableName}";
        return AppendWhere(sql, where);
    }

    #region Protected virtual

    protected virtual string AppendWhere(string sql,
        WhereConditions where = null)
    {
        if (where == null || where.Items.Count < 1)
        {
            return sql;
        }

        var whereBuilder = new StringBuilder();
        foreach (var item in where.Items)
        {
            if (whereBuilder.Length > 0)
            {
                whereBuilder.Append(item.WhereConnecter == WhereConnecter.And ? " AND " : " OR ");
            }

            whereBuilder.Append($"{item.Column}");

            if (item is WhereConditionsNullItem nullCondition)
            {
                whereBuilder.Append($" IS {(nullCondition.IsNull ? "NULL" : "NOT NULL")}");
            }
            else if (item is WhereConditionsItem condition)
            {
                switch (condition.WhereOperator)
                {
                    case WhereOperator.Equal:
                        whereBuilder.Append(" = ");
                        break;
                    case WhereOperator.NotEqual:
                        whereBuilder.Append(" != ");
                        break;
                    case WhereOperator.GreaterThan:
                        whereBuilder.Append(" > ");
                        break;
                    case WhereOperator.GreaterEqual:
                        whereBuilder.Append(" >= ");
                        break;
                    case WhereOperator.LessThan:
                        whereBuilder.Append(" < ");
                        break;
                    case WhereOperator.LessEqual:
                        whereBuilder.Append(" <= ");
                        break;
                    case WhereOperator.Like:
                        whereBuilder.Append(" LIKE ");
                        break;
                }
                whereBuilder.Append(GetParamName(condition.Param));
            }
        }

        return $"{sql} WHERE {whereBuilder}";
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

    protected internal virtual string GetParamName(ISqlParameter param)
    {
        return $"{ParamPrefix}{param.ParamName}";
    }

    #endregion
}