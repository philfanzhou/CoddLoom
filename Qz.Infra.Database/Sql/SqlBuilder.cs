using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Qz.Infra.Database.Sql;

public partial class SqlBuilder
{
    protected const string DbParameterPrefix = "@";
    protected const string KeyWordSelect = "SELECT";
    protected const string KeyWordWhere = "WHERE";

    public virtual string Insert(string tableName, InputValues input)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (input == null || input.IsEmpty()) throw new ArgumentNullException(nameof(input));

        var columns = $"({string.Join(",", input.Items.Select(p => p.Column))})";
        var values = $"({string.Join(",", input.Items.Select(GetParamName))})";

        return $"INSERT INTO {tableName} {columns} VALUES {values}";
    }

    public virtual string Insert(string tableName, IEnumerable<InputValues> inputs)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if(inputs == null) throw new ArgumentNullException(nameof(inputs));
        var inputList = inputs.ToList();
        if(inputList.Count < 1 || inputList.Any(p => p == null || p.IsEmpty())) throw new ArgumentNullException(nameof(inputs));

        var columns = $"({string.Join(",", inputList[0].Items.Select(p => p.Column))})";
        var valueList = new List<string>();
        foreach(var input in inputList)
        {
            valueList.Add($"({string.Join(",", input.Items.Select(GetParamName))})");
        }
        var values = $"{string.Join(",", valueList.Select(p => p))}";

        return $"INSERT INTO {tableName} {columns} VALUES {values}";
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

        var sql = $"{KeyWordSelect} COUNT({column}) FROM {tableName}";
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

        return $"{sql} {KeyWordWhere} {where.GetWhereString(this)}";
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

    protected internal virtual string GetParamName(ColumnValueParameter param)
    {
        return $"{DbParameterPrefix}{param.ParamName}";
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
}