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

    public virtual string Insert(string tableName, IEnumerable<InputValues> inputs, out List<ColumnValueParameter> dbParams,  
        bool useParameter = true)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if(inputs == null) throw new ArgumentNullException(nameof(inputs));
        var inputList = inputs.ToList();
        if(inputList.Count < 1 || inputList.Any(p => p == null || p.IsEmpty())) throw new ArgumentNullException(nameof(inputs));

        var columnSql = GetInsertColumns(inputList[0].Items);

        dbParams = new List<ColumnValueParameter>();
        var valueList = new List<string>();
        foreach(var input in inputList)
        {
            var values = GetInsertValues(input.Items, out var innerDbParams, useParameter);
            valueList.Add(values);
            dbParams.AddRange(innerDbParams);
        }
        var valueSql = $"{string.Join(",", valueList.Select(p => p))}";

        return $"INSERT INTO {tableName} {columnSql} VALUES {valueSql}";
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

    protected virtual string GetInsertValue(ColumnValueParameter param)
    {
        if(param.Value == DBNull.Value)
        {
            return "NULL";
        }
        else if(param.Value is string)
        {
            return $"'{param.Value.ToString().Replace("'", "''")}'";
        }
        else if(param.Value is DateTime)
        {
            return $"'{param.Value:yyyy-MM-dd HH:mm:ss}'";
        }
        else
        {
            return param.Value.ToString();
        }
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

    protected virtual string GetInsertColumns(IEnumerable<ColumnValueParameter> parameters)
    {
        var columns = $"({string.Join(",", parameters.Select(p => p.Column))})";
        return columns;
    }

    protected virtual string GetInsertValues(IEnumerable<ColumnValueParameter> parameters,
        out List<ColumnValueParameter> dbParams, bool useParameter = true)
    {
        dbParams = new List<ColumnValueParameter>();
        var valuesStrBuilder = new StringBuilder();
        foreach(var item in parameters)
        {
            if(valuesStrBuilder.Length > 0)
            {
                valuesStrBuilder.Append(",");
            }
            if (useParameter || item.ForceParameter)
            {
                valuesStrBuilder.Append(GetParamName(item));
                dbParams.Add(item);
            }
            else
            {
                valuesStrBuilder.Append(GetInsertValue(item));
            }
        }

        return $"({valuesStrBuilder})";
    }

    #endregion
}