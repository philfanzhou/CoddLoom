using Qz.Infra.Database.Condition;
using Qz.Infra.Database.Input;
using Qz.Infra.Database.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qz.Infra.Database.Sql;

public partial class SqlBuilder
{
    protected const string DbParameterPrefix = "@";

    public virtual string Insert(string tableName, IEnumerable<InputValues> inputs, out List<ValueParam> dbParams,
        bool useParameter = true)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (inputs == null) throw new ArgumentNullException(nameof(inputs));

        var inputList = inputs.ToList();
        if (inputList.Count < 1 || inputList.Any(p => p == null || p.IsEmpty()))
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        var columnSql = GetInsertColumns(inputList[0].Items);

        dbParams = new List<ValueParam>();
        var valueList = new List<string>();
        foreach (var input in inputList)
        {
            var values = GetInsertValues(input.Items, out var innerDbParams, useParameter);
            valueList.Add(values);
            dbParams.AddRange(innerDbParams);
        }
        var valueSql = $"{string.Join(",", valueList.Select(p => p))}";

        return $"INSERT INTO {tableName} {columnSql} VALUES {valueSql}";
    }

    public virtual string Delete(string tableName, WhereConditions where, bool force = false)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if ((where == null || where.IsEmpty()) && !force) throw new ArgumentNullException(nameof(where));

        var sql = $"DELETE FROM {tableName}";
        return AppendWhere(sql, where);
    }

    public virtual string Update(string tableName, InputValues input, WhereConditions where, bool force = false)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
        if (input == null || input.IsEmpty()) throw new ArgumentNullException(nameof(input));
        if ((where == null || where.IsEmpty()) && !force) throw new ArgumentNullException(nameof(where));

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

    public virtual string Count(string tableName,
        WhereConditions where = null, ColumnParam columns = null)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

        var column = "*";
        if (where?.Parameters.FirstOrDefault() != null)
        {
            // 只查询where条件的第一个column，提高性能
            column = where.Parameters.First().Column;
        }
        else if (columns?.Select.FirstOrDefault() != null)
        {
            column = columns.Select.First().Column;
        }

        var sql = $"SELECT COUNT({column}) FROM {tableName}";
        sql = AppendWhere(sql, where);
        return AppendGroupBy(sql, columns);
    }

    public virtual string Select(string tableName, 
        WhereConditions where = null, OrderByCondition orderBy = null, PageParam pageParam = null, ColumnParam columns = null)
    {
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

        var sql = $"SELECT {GetSelectColumnSql(columns)} FROM {tableName}";
        sql = AppendWhere(sql, where);
        sql = AppendGroupBy(sql, columns);
        sql = AppendOrderBy(sql, orderBy);
        return AppendLimit(sql, pageParam);
    }

    #region Protected virtual

    protected virtual string AppendGroupBy(string sql, 
        ColumnParam columns = null)
    {
        if (columns == null || columns.GroupBy.Count < 1) return sql;
        return $"{sql} GROUP BY {string.Join(",", columns.GroupBy.Select(p => p.Column))}";
    }

    protected virtual string AppendOrderBy(string sql,
        OrderByCondition orderBy = null)
    {
        if (orderBy == null || orderBy.IsEmpty()) return sql;
        var condition = string.Join(",", orderBy.Items.Select(p =>
        $"{p.Column} {(p.Descending ? "DESC" : "ASC")}"));
        return $"{sql} ORDER BY {condition}";
    }

    protected virtual string AppendLimit(string sql,
        PageParam pageParam = null)
    {
        if (pageParam == null) return sql;
        return $"{sql} LIMIT {pageParam.Offset},{pageParam.PageSize}";
    }

    protected virtual string GetInsertValues(IEnumerable<ValueParam> parameters,
        out List<ValueParam> dbParams, bool useParameter = true)
    {
        dbParams = new List<ValueParam>();
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
                valuesStrBuilder.Append(GetInsertValueString(item));
            }
        }

        return $"({valuesStrBuilder})";
    }

    protected virtual string GetInsertValueString(ValueParam param)
    {
        if (param.Value == DBNull.Value)
        {
            return "NULL";
        }
        else if (param.Value is string)
        {
            return $"'{param.Value.ToString().Replace("'", "''")}'";
        }
        else if (param.Value is DateTime)
        {
            return $"'{param.Value:yyyy-MM-dd HH:mm:ss}'";
        }
        else if (param.Value is bool v)
        {
            return v ? "1" : "0";
        }
        else
        {
            return param.Value.ToString();
        }
    }

    protected internal virtual string GetParamName(ValueParam param)
    {
        return $"{DbParameterPrefix}{param.ParamName}";
    }

    #endregion
}